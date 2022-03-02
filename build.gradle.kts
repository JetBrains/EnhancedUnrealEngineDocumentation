import org.apache.tools.ant.taskdefs.condition.Os
import org.jetbrains.intellij.tasks.PrepareSandboxTask
import org.jetbrains.intellij.tasks.RunIdeTask
import org.jetbrains.kotlin.gradle.tasks.KotlinCompile
import org.gradle.api.tasks.testing.logging.TestExceptionFormat
import java.io.ByteArrayOutputStream

buildscript {
    repositories {
        maven { setUrl("https://cache-redirector.jetbrains.com/repo.maven.apache.org/maven2")}
    }
    dependencies {
        classpath("com.jetbrains.rd:rd-gen:2022.1.2")
    }
}

gradle.startParameter.showStacktrace = ShowStacktrace.ALWAYS

plugins {
    kotlin("jvm") version "1.6.10"
    id("com.jetbrains.rdgen") version "2022.1.2"
    id("me.filippov.gradle.jvm.wrapper") version "0.9.3"
    id("org.jetbrains.changelog") version "1.3.1"
    id("org.jetbrains.intellij") version "1.4.0"
}

apply {
    plugin("kotlin")
    plugin("com.jetbrains.rdgen")
}

repositories {
    maven { setUrl("https://cache-redirector.jetbrains.com/intellij-repository/snapshots") }
    maven { setUrl("https://cache-redirector.jetbrains.com/maven-central") }
}

kotlin {
    sourceSets {
        main {
            kotlin.srcDir("src/rider/main/kotlin")
        }
        test {
            kotlin.srcDir("src/rider/test/kotlin")
        }
    }
}

sourceSets {
    main {
        resources.srcDir("src/rider/main/resources")
    }
}

project.version = "${property("majorVersion")}." +
        "${property("minorVersion")}." +
        "${property("buildCounter")}"

if (System.getenv("TEAMCITY_VERSION") != null) {
    logger.lifecycle("##teamcity[buildNumber '${project.version}']")
} else {
    logger.lifecycle("Plugin version: ${project.version}")
}

val buildConfigurationProp = project.property("buildConfiguration").toString()

val repoRoot by extra { project.rootDir }
val isWindows by extra { Os.isFamily(Os.FAMILY_WINDOWS) }
val idePluginId by extra { "RiderPlugin" }
val dotNetSolutionId by extra { "DocsByBenUI" }
val dotNetDir by extra { File(repoRoot, "src/dotnet") }
val dotNetBinDir by extra { dotNetDir.resolve("$idePluginId.$dotNetSolutionId").resolve("bin") }
val dotNetPluginId by extra { "$idePluginId.${project.name}" }
val dotNetSolution by extra { File(repoRoot, "$dotNetSolutionId.sln") }
val modelDir = File(repoRoot, "protocol/src/main/kotlin/model")
val hashBaseDir = File(repoRoot, "build/rdgen")
val csOutputRoot = File(repoRoot, "src/dotnet/RiderPlugin.DocsByBenUI/obj/model")
val ktOutputRoot = File(repoRoot, "src/rider/main/kotlin/com/jetbrains/rider/model")

val currentBranchName = getBranchName()

fun TaskContainerScope.setupCleanup(task: Task) {
    withType<Delete> {
        delete(task.outputs.files)
    }
}

fun getBranchName(): String {
    val stdOut = ByteArrayOutputStream()
    val result = project.exec {
        executable = "git"
        args = listOf("rev-parse", "--abbrev-ref", "HEAD")
        workingDir = projectDir
        standardOutput = stdOut
    }
    if (result.exitValue == 0) {
        val output = stdOut.toString().trim()
        if (output.isNotEmpty())
            return output
    }
    return "net221"
}

changelog {
    version.set(project.version.toString())
    // https://github.com/JetBrains/gradle-changelog-plugin/blob/main/src/main/kotlin/org/jetbrains/changelog/Changelog.kt#L23
    // This is just common semVerRegex with the addition of a forth optional group (number) ( x.x.x[.x][-alpha43] )
    headerParserRegex.set(
        """^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)\.?(0|[1-9]\d*)?(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)
            (?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?${'$'}"""
            .trimMargin().toRegex())
    groups.set(listOf("Added", "Changed", "Deprecated", "Removed", "Fixed", "Known Issues"))
    keepUnreleasedSection.set(true)
    itemPrefix.set("-")
}

intellij {
    type.set("RD")
    instrumentCode.set(false)
    downloadSources.set(false)

    plugins.set(listOf("com.jetbrains.rider-cpp"))

    val dependencyPath = File(projectDir, "dependencies")
    if (dependencyPath.exists()) {
        localPath.set(dependencyPath.canonicalPath)
        println("Will use ${File(localPath.get(), "build.txt").readText()} from $localPath as RiderSDK")
    } else {
        version.set("${project.property("majorVersion")}-SNAPSHOT")
        println("Will download and use build/riderRD-${version.get()} as RiderSDK")
    }

    tasks {
        val currentReleaseNotesAsHtml = """
            <body>
            <p><b>New in "${project.version}"</b></p>
            <p>${changelog.getLatest().toHTML()}</p>
            <p>See the <a href="https://github.com/JetBrains/DocsByBenUI/blob/$currentBranchName/CHANGELOG.md">CHANGELOG</a> for more details and history.</p>
            </body>
        """.trimIndent()

        val currentReleaseNotesAsMarkdown = """
            ## New in ${project.version}
            ${changelog.getLatest().toText()}
            See the [CHANGELOG](https://github.com/JetBrains/DocsByBenUI/blob/$currentBranchName/CHANGELOG.md) for more details and history.
        """.trimIndent()
        val dumpCurrentChangelog by registering {
            val outputFile = File("${project.buildDir}/release_notes.md")
            outputs.file(outputFile)
            doLast { outputFile.writeText(currentReleaseNotesAsMarkdown) }
        }

        // PatchPluginXml gets latest (always Unreleased) section from current changelog and write it into plugin.xml
        // dumpCurrentChangelog dumps the same section to file (for Marketplace changelog)
        // After, patchChangelog rename [Unreleased] to [202x.x.x.x] and create new empty Unreleased.
        // So order is important!
        patchPluginXml { changeNotes.set( provider { currentReleaseNotesAsHtml }) }
        patchChangelog { mustRunAfter(patchPluginXml, dumpCurrentChangelog) }

        publishPlugin {
            dependsOn(patchPluginXml, dumpCurrentChangelog, patchChangelog)
            token.set(System.getenv("DocsByBenUI_intellijPublishToken"))

            val pubChannels = project.findProperty("publishChannels")
            if ( pubChannels != null) {
                val chan = pubChannels.toString().split(',')
                println("Channels for publish $chan")
                channels.set(chan)
            } else {
                channels.set(listOf("alpha"))
            }
        }
    }
}

tasks {
    val dotNetSdkPath by lazy {
        val sdkPath = setupDependencies.get().idea.get().classes.resolve("lib").resolve("DotNetSdkForRdPlugins")
        assert(sdkPath.isDirectory)
        println(".NET SDK path: $sdkPath")

        return@lazy sdkPath.canonicalPath
    }

    val riderModelJar by lazy {
        val rdLib = setupDependencies.get().idea.get().classes.resolve("lib").resolve("rd")
        assert(rdLib.isDirectory)
        val jarFile = File(rdLib, "rider-model.jar")
        assert(jarFile.isFile)
        return@lazy jarFile.canonicalPath
    }

    withType<RunIdeTask> {
        maxHeapSize = "4096m"
    }

    withType<Test> {
        useTestNG()
        testLogging {
            showStandardStreams = true
            showExceptions = true
            exceptionFormat = TestExceptionFormat.FULL
        }
    }

    withType<KotlinCompile> {
        kotlinOptions {
            jvmTarget = "11"
        }
    }

    val prepareRiderBuildProps by registering {
        group = "RiderBackend"
        val generatedFile = project.buildDir.resolve("DotNetSdkPath.generated.props")

        inputs.property("dotNetSdkFile", { dotNetSdkPath })
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
        inputs.property("dotNetSdkFile", { dotNetSdkPath })
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
        outputs.dir(file("$repoRoot/src/dotnet/RiderPlugin.DocsByBenUI/bin/RiderPlugin.DocsByBenUI/$buildConfigurationProp"))

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
                executable = "$rootDir/tools/dotnet.cmd"
                args = buildArguments
                workingDir = dotNetSolution.parentFile
            }
        }
    }

    withType<PrepareSandboxTask> {
        dependsOn(buildResharperHost)

        outputs.upToDateWhen { false } //need to dotnet artifacts be included when only dotnet sources were changed

        val outputFolder = dotNetBinDir
            .resolve(dotNetPluginId)
            .resolve(buildConfigurationProp)

        val dllFiles = listOf(
            File(outputFolder, "$dotNetPluginId.dll"),
            File(outputFolder, "$dotNetPluginId.pdb")
        )

        dllFiles.forEach {
            from(it) { into("${intellij.pluginName.get()}/dotnet") }
        }

        doLast {
            dllFiles.forEach { file ->
                if (!file.exists()) throw RuntimeException("File $file does not exist")
            }
        }
    }
}

