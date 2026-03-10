import java.util.zip.ZipFile
import org.gradle.api.tasks.testing.logging.TestExceptionFormat
import org.jetbrains.changelog.Changelog
import org.jetbrains.intellij.platform.gradle.Constants
import org.jetbrains.intellij.platform.gradle.tasks.PrepareSandboxTask
import org.jetbrains.intellij.platform.gradle.tasks.RunIdeTask
import kotlin.io.path.absolute
import kotlin.io.path.isDirectory

fun properties(key: String) = project.findProperty(key).toString()

plugins {
    id("java") // Java support
    alias(libs.plugins.kotlin) // Kotlin support
    alias(libs.plugins.intelliJPlatform) // IntelliJ Platform Gradle Plugin
    alias(libs.plugins.changelog) // Gradle Changelog Plugin
    alias(libs.plugins.qodana) // Gradle Qodana Plugin
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
        rider(properties("platformVersion")) {
            useInstaller = false
        }
        jetbrainsRuntime()
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
val idePluginId by extra { "RiderPlugin" }
val dotNetSolutionId by extra { "EnhancedUnrealEngineDocumentation" }
val dotNetDir by extra { File(repoRoot, "src/dotnet") }
val dotNetBinDir by extra { dotNetDir.resolve("$idePluginId.$dotNetSolutionId").resolve("bin") }
val dotNetPluginId by extra { "$idePluginId.${project.name}" }
val yamlParsingId = "YamlDocsParsing"
val dotNetSolution by extra { File(repoRoot, "$dotNetSolutionId.sln") }

val dotNetSdkPath by lazy {
    val path = intellijPlatform.platformPath.resolve("lib/DotNetSdkForRdPlugins").absolute()
    if (!path.isDirectory()) error("$path does not exist or not a directory")

    println("Rider SDK path: $path")
    return@lazy path
}

val pluginStagingDir = layout.buildDirectory.dir("plugin-staging").get().asFile
val pluginStagingContentDir = file("${pluginStagingDir}/${rootProject.name}")
val signingManifestFile = file("${pluginStagingDir}/files-to-sign.txt")

// All .NET files to include in the plugin
val dotNetOutputFiles = listOf(
    "${dotNetPluginId}.dll",
    "${dotNetPluginId}.pdb",
    "${yamlParsingId}.dll",
    "${yamlParsingId}.pdb",
)

// .NET files that need signing — only our own code
val dotNetFilesToSign = listOf(
    "${dotNetPluginId}.dll",
    "${yamlParsingId}.dll",
)

// JAR files that need signing
val jarFilesToSign = buildList {
    add("${rootProject.name}-${version}.jar")
    if (intellijPlatform.buildSearchableOptions.get()) {
        add("${rootProject.name}-${version}-searchableOptions.jar")
    }
}

tasks.withType<RunIdeTask>().configureEach {
    jvmArgs("-Didea.reset.classpath.from.manifest=true")
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

kotlin {
    jvmToolchain(properties("javaVersion").toInt())
}

tasks.named("buildSearchableOptions").configure {
    enabled = false
}

tasks {
    // Set the JVM compatibility versions
    properties("javaVersion").let {
        withType<JavaCompile> {
            sourceCompatibility = it
            targetCompatibility = it
        }
    }

    wrapper {
        gradleVersion = properties("gradleVersion")
    }

    patchPluginXml {
        sinceBuild.set(properties("pluginSinceBuild"))

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
        val generatedFile = layout.buildDirectory.file("DotNetSdkPath.generated.props")

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

    val buildResharperHost by registering(Exec::class) {
        group = "RiderBackend"
        description = "Build backend for Rider"
        dependsOn(prepareNuGetConfig)

        inputs.file(file(dotNetSolution))
        inputs.dir(file("$repoRoot/src/dotnet"))
        outputs.dir(file("$repoRoot/src/dotnet/RiderPlugin.EnhancedUnrealEngineDocumentation/bin/RiderPlugin.EnhancedUnrealEngineDocumentation/$buildConfigurationProp"))

        val warningsAsErrors: String by project.extra
        val buildArguments = listOf(
            "build",
            dotNetSolution.canonicalPath,
            "-consoleLoggerParameters:ErrorsOnly",
            "/p:Configuration=$buildConfigurationProp",
            "/p:Version=${project.version}",
            "/p:TreatWarningsAsErrors=$warningsAsErrors",
            "/bl:${dotNetSolution.name}.binlog",
            "/nologo"
        )
        logger.info("call dotnet.cmd with '$buildArguments'")
        executable = file("$rootDir/tools/dotnet.cmd").normalize().canonicalPath
        args = buildArguments
        workingDir = dotNetSolution.parentFile
    }

    val copyDocs by registering(Copy::class) {
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

        dotNetOutputFiles.forEach { fileName ->
            from(File(outputFolder, fileName)) {
                into("${rootProject.name}/dotnet")
            }
        }

        from(copyDocs) {
            into("${rootProject.name}/documentation")
        }

        doLast {
            dotNetOutputFiles.forEach { fileName ->
                val file = File(outputFolder, fileName)
                if (!file.exists()) throw RuntimeException("File $file does not exist")
            }
        }
    }

    // ========= Two-Phase Build for Signing Support ================

    // Preparation for plugin internals signing. Build all JARs and put them into ${pluginStagingDir}
    val preparePluginInternalsForSigning by registering(Sync::class) {
        description = "Prepares plugin files for signing and generates signing manifest"
        group = "build"

        // Source 1: Copy the full plugin directory from prepareSandbox
        from(prepareSandbox.map { it.pluginDirectory })

        // Source 2: Copy searchable options JAR to lib/ (when enabled)
        if (intellijPlatform.buildSearchableOptions.get()) {
            from(jarSearchableOptions.map { it.archiveFile }) {
                into("lib")
            }
        }

        // Destination: the plugin content directory inside staging
        into(pluginStagingContentDir)

        // Capture script-level vals into locals to avoid capturing the build script in doLast
        val signingManifestFile = signingManifestFile
        val pluginStagingDir = pluginStagingDir
        val jarFilesToSign = jarFilesToSign
        val dotNetFilesToSign = dotNetFilesToSign
        val projectName = rootProject.name

        // After syncing, generate the signing manifest
        doLast {
            val filesToSign = mutableListOf<String>()

            // Add JAR files that need signing
            jarFilesToSign.forEach { jarName ->
                filesToSign.add("${projectName}/lib/${jarName}")
            }

            // Add .NET files that need signing
            dotNetFilesToSign.forEach { fileName ->
                filesToSign.add("${projectName}/dotnet/${fileName}")
            }

            // Write manifest
            signingManifestFile.writeText(filesToSign.joinToString("\n"))

            // Summary
            println("Plugin prepared for signing: ${pluginStagingDir}")
            println("Signing manifest: ${signingManifestFile}")
            println("Files to sign: ${filesToSign.size}")
            filesToSign.forEach { println("  - $it") }
        }
    }

    // Validates that ${pluginStagingDir} has all required files to assemble the plugin
    val validatePluginStaging by registering {
        description = "Validates that plugin staging directory exists and contains required files"
        group = "build"

        // Capture script-level vals into locals to avoid capturing the build script in doLast
        val pluginStagingContentDir = pluginStagingContentDir
        val dotNetOutputFiles = dotNetOutputFiles
        val jarFilesToSign = jarFilesToSign

        doLast {
            if (!pluginStagingContentDir.exists()) {
                throw RuntimeException(
                    "Plugin staging directory not found: ${pluginStagingContentDir}\n" +
                    "Run './gradlew preparePluginInternalsForSigning' first."
                )
            }

            // Validate expected .NET output files exist
            dotNetOutputFiles.forEach { fileName ->
                val file = pluginStagingContentDir.resolve("dotnet/${fileName}")
                if (!file.exists()) throw RuntimeException("Expected .NET file not found: ${file}")
            }

            // Validate expected JAR files exist
            jarFilesToSign.forEach { jarName ->
                val file = pluginStagingContentDir.resolve("lib/${jarName}")
                if (!file.exists()) throw RuntimeException("Expected JAR file not found: ${file}")
            }
        }
    }

    // Assembles the final zip-archive from staged (potentially externally signed) files.
    // Produces a ZIP with "-from-staging" suffix by default (override with -PoutputPluginFileSuffix=<value>)
    // Can be used in pipeline: preparePluginInternalsForSigning -> external sign -> assemblePlugin
    val assemblePlugin by registering(Zip::class) {
        description = "Assembles the plugin ZIP from staged files with '-from-staging' classifier"
        group = "build"

        dependsOn(validatePluginStaging)

        from(pluginStagingDir)
        include("${rootProject.name}/**")
        exclude("files-to-sign.txt")

        archiveBaseName.convention(intellijPlatform.projectName)
        archiveClassifier.set(providers.gradleProperty("outputPluginFileSuffix").orElse("from-staging"))
        destinationDirectory.set(layout.buildDirectory.dir("distributions"))
    }

    // ==============================================================

    // buildPlugin keeps its default Zip behavior (sources from prepareSandbox + jarSearchableOptions).
    // We add dependsOn(preparePluginInternalsForSigning) to ensure the staging directory is populated,
    // then verify the archive matches the staging directory.
    buildPlugin {
        dependsOn(preparePluginInternalsForSigning)

        val pluginStagingContentDir = pluginStagingContentDir
        val projectName = rootProject.name

        doLast {
            // Verify the archive matches the staging directory to be sure that
            // buildPlugin and preparePluginInternalsForSigning+assemblePlugin produces the same results
            val zipFiles = ZipFile(archiveFile.get().asFile).use {
                it.entries().asSequence().filterNot { e -> e.isDirectory }.map { e -> e.name }.sorted().toList()
            }
            val stagingFiles = pluginStagingContentDir.walkTopDown().filter { it.isFile }
                .map { "${projectName}/${it.relativeTo(pluginStagingContentDir).path.replace('\\', '/')}" }
                .sorted().toList()

            check(zipFiles == stagingFiles) {
                "Plugin archive and staging directory are out of sync!\n" +
                "  Only in archive: ${zipFiles - stagingFiles.toSet()}\n" +
                "  Only in staging: ${stagingFiles - zipFiles.toSet()}"
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
        intellijPlatform.platformPath.resolve("lib/rd/rider-model.jar")
    }) {
        builtBy(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
    }
}

