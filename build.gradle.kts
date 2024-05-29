import kotlin.io.path.absolute
import kotlin.io.path.isDirectory
import org.apache.tools.ant.taskdefs.condition.Os
import org.gradle.api.tasks.testing.logging.TestExceptionFormat
import org.jetbrains.changelog.Changelog
import org.jetbrains.intellij.platform.gradle.Constants
import org.jetbrains.intellij.platform.gradle.tasks.PrepareSandboxTask
import org.jetbrains.kotlin.gradle.tasks.KotlinCompile

fun properties(key: String) = project.findProperty(key).toString()

plugins {
    id("idea")
    // Kotlin support
    kotlin("jvm") version "1.9.24" // https://mvnrepository.com/artifact/org.jetbrains.kotlin.jvm/org.jetbrains.kotlin.jvm.gradle.plugin
    // Gradle IntelliJ Plugin
    id("org.jetbrains.intellij.platform") version "2.0.0-beta3" // https://github.com/JetBrains/gradle-intellij-plugin/releases
    // Gradle Changelog Plugin
    id("org.jetbrains.changelog") version "2.2.0"
    // Gradle Qodana Plugin
    id("org.jetbrains.qodana") version "0.1.13"
    id("me.filippov.gradle.jvm.wrapper") version "0.14.0"
}

allprojects {
    repositories {
        mavenCentral()
    }
}

gradle.startParameter.showStacktrace = ShowStacktrace.ALWAYS

group = properties("pluginGroup")
version = properties("pluginVersion")

// Configure project's dependencies
repositories {
    mavenCentral()

    intellijPlatform {
        defaultRepositories()
        jetbrainsRuntime()
    }
}

dependencies {
    intellijPlatform {
        rider(properties("platformVersion"))
        jetbrainsRuntime()
        instrumentationTools()
        bundledPlugin("com.jetbrains.rider-cpp")
    }
}

kotlin {
    jvmToolchain {
        languageVersion = JavaLanguageVersion.of(17)
    }
}

sourceSets {
    main {
        kotlin.srcDir("src/rider/main")
        resources.srcDir("src/rider/main/resources")
    }
}

val buildConfigurationProp = properties("buildConfiguration")

val repoRoot by extra { project.rootDir }
val isWindows by extra { Os.isFamily(Os.FAMILY_WINDOWS) }
val idePluginId by extra { "RiderPlugin" }
val dotNetSolutionId by extra { "EnhancedUnrealEngineDocumentation" }
val dotNetDir by extra { File(repoRoot, "src/dotnet") }
val dotNetBinDir by extra { dotNetDir.resolve("$idePluginId.$dotNetSolutionId").resolve("bin") }
val dotNetPluginId by extra { "$idePluginId.${project.name}" }
val dotNetSolution by extra { File(repoRoot, "$dotNetSolutionId.sln") }
val modelDir = File(repoRoot, "protocol/src/main/kotlin/model")
val hashBaseDir = File(repoRoot, "build/rdgen")
val csOutputRoot = File(repoRoot, "src/dotnet/RiderPlugin.EnhancedUnrealEngineDocumentation/obj/model")
val ktOutputRoot = File(repoRoot, "src/rider/main/kotlin/com/jetbrains/rider/model")

val dotNetSdkPath by lazy {
    val path = intellijPlatform.platformPath.resolve("lib/DotNetSdkForRdPlugins").absolute()
    if (!path.isDirectory()) error("$path does not exist or not a directory")

    println("Rider SDK path: $path")
    return@lazy path
}

fun TaskContainerScope.setupCleanup(task: Task) {
    withType<Delete> {
        delete(task.outputs.files)
    }
}


// Configure Gradle Changelog Plugin - read more: https://github.com/JetBrains/gradle-changelog-plugin
changelog {
    version.set(properties("pluginVersion"))
    path.set(file("CHANGELOG.md").canonicalPath)
    itemPrefix.set("-")
    keepUnreleasedSection.set(true)
    unreleasedTerm.set("[Unreleased]")
    groups.set(listOf("Added", "Changed", "Deprecated", "Removed", "Fixed", "Security"))
    lineSeparator.set("\n")
}

tasks {
    // Set the JVM compatibility versions
    properties("javaVersion").let {
        withType<JavaCompile> {
            sourceCompatibility = it
            targetCompatibility = it
        }
        withType<KotlinCompile> {
            kotlinOptions.jvmTarget = it
        }
    }

    wrapper {
        gradleVersion = properties("gradleVersion")
    }

    patchPluginXml {
        sinceBuild.set(properties("pluginSinceBuild"))
        untilBuild.set(properties("pluginUntilBuild"))

        // Extract the <!-- Plugin description --> section from README.md and provide for the plugin's manifest
        pluginDescription.set(
                projectDir.resolve("README.md").readText().lines().run {
                    val start = "<!-- Plugin description -->"
                    val end = "<!-- Plugin description end -->"

                    if (!containsAll(listOf(start, end))) {
                        throw GradleException("Plugin description section not found in README.md:\n$start ... $end")
                    }
                    subList(indexOf(start) + 1, indexOf(end))
                }.joinToString("\n").run { org.jetbrains.changelog.markdownToHTML(this) }
        )

        // Get the latest available change notes from the changelog file
        changeNotes.set(provider {
            changelog.renderItem(
                    changelog.run{
                        getOrNull(properties("pluginVersion")) ?: getUnreleased()
                    }
                            .withHeader(false)
                            .withEmptySections(false),
                    Changelog.OutputType.HTML
            )
        })
    }

    publishPlugin {
        dependsOn("patchChangelog")
        token.set(System.getenv("PUBLISH_TOKEN"))
        // pluginVersion is based on the SemVer (https://semver.org) and supports pre-release labels, like 2.1.7-alpha.3
        // Specify pre-release label to publish the plugin in a custom Release Channel automatically. Read more:
        // https://plugins.jetbrains.com/docs/intellij/deployment.html#specifying-a-release-channel
        channels.set(listOf(properties("pluginVersion").split('-').getOrElse(1) { "default" }.split('.').first()))
    }



    runIde {
        jvmArgs("-Xmx4096m")
    }

    withType<Test> {
        useTestNG()
        testLogging {
            showStandardStreams = true
            showExceptions = true
            exceptionFormat = TestExceptionFormat.FULL
        }
    }

    val prepareRiderBuildProps by registering {
        group = "RiderBackend"
        val generatedFile = project.buildDir.resolve("DotNetSdkPath.generated.props")

//        inputs.property("dotNetSdkFile", { dotNetSdkPath })
        outputs.file(generatedFile)

        doLast {
            project.file(generatedFile).writeText(
                """<Project>
            |  <PropertyGroup>
            |    <DotNetSdkPath>$dotNetSdkPath</DotNetSdkPath>
            |  </PropertyGroup>
            |</Project>""".trimMargin()
            )
        }
    }

    val prepareNuGetConfig by registering {
        group = "RiderBackend"
        dependsOn(prepareRiderBuildProps)

        val generatedFile = project.projectDir.resolve("NuGet.Config")
//        inputs.property("dotNetSdkFile", { dotNetSdkPath })
        outputs.file(generatedFile)
        doLast {
            val dotNetSdkFile = dotNetSdkPath
            logger.info("dotNetSdk location: '$dotNetSdkFile'")

            val nugetConfigText = """<?xml version="1.0" encoding="utf-8"?>
        |<configuration>
        |  <packageSources>
        |    <clear />
        |    <add key="local-dotnet-sdk" value="$dotNetSdkFile" />
        |    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
        |  </packageSources>
        |</configuration>
        """.trimMargin()
            generatedFile.writeText(nugetConfigText)

            logger.info("Generated content:\n$nugetConfigText")
        }
    }

    val buildResharperHost by registering {
        group = "RiderBackend"
        description = "Build backend for Rider"
        dependsOn(prepareNuGetConfig)

        inputs.file(file(dotNetSolution))
        inputs.dir(file("$repoRoot/src/dotnet"))
        outputs.dir(file("$repoRoot/src/dotnet/RiderPlugin.EnhancedUnrealEngineDocumentation/bin/RiderPlugin.EnhancedUnrealEngineDocumentation/$buildConfigurationProp"))

        doLast {
            val warningsAsErrors: String by project.extra
            val buildArguments = listOf(
                "build",
                dotNetSolution.canonicalPath,
                "/p:Configuration=$buildConfigurationProp",
                "/p:Version=${project.version}",
                "/p:TreatWarningsAsErrors=$warningsAsErrors",
                "/v:${project.properties.getOrDefault("dotnetVerbosity", "minimal")}",
                "/bl:${dotNetSolution.name}.binlog",
                "/nologo"
            )
            logger.info("call dotnet.cmd with '$buildArguments'")
            project.exec {
                executable = file("$rootDir/tools/dotnet.cmd").normalize().canonicalPath
                args = buildArguments
                workingDir = dotNetSolution.parentFile
            }
        }
    }

    val copyDocs by creating(Copy::class) {
        from("$repoRoot/documentation")
        into("$repoRoot/build/distributions/documentation")
    }

    withType<PrepareSandboxTask> {
        dependsOn(buildResharperHost)
        dependsOn(copyDocs)

        outputs.upToDateWhen { false } //need to dotnet artifacts be included when only dotnet sources were changed

        val outputFolder = dotNetBinDir
            .resolve(dotNetPluginId)
            .resolve(buildConfigurationProp)

        val dllFiles = listOf(
            File(outputFolder, "$dotNetPluginId.dll"),
            File(outputFolder, "$dotNetPluginId.pdb")
        )

        from(dllFiles) {
            into("${rootProject.name}/dotnet")
        }

        from(copyDocs.outputs.files.first()) {
            into("${rootProject.name}/documentation")
        }

        doLast {
            dllFiles.forEach { file ->
                if (!file.exists()) throw RuntimeException("File $file does not exist")
            }
        }
    }
}


val riderModel: Configuration by configurations.creating {
    isCanBeConsumed = true
    isCanBeResolved = false
}

artifacts {
    add(riderModel.name, provider {
        intellijPlatform.platformPath.resolve("lib/rd/rider-model.jar").also {
//            check(it.isFile) {
//                "rider-model.jar is not found at $riderModel"
//            }
        }
    }) {
        builtBy(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
    }
}

