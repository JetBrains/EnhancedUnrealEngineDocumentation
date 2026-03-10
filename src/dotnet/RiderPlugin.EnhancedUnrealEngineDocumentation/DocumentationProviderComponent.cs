using System.Collections.Generic;
using System.Reflection;
using JetBrains.Application;
using JetBrains.Application.Environment;
using JetBrains.Application.Environment.DeployedPackages;
using JetBrains.Util;
using JetBrains.Util.Logging;
using YamlDocsParsing;

namespace RiderPlugin.EnhancedUnrealEngineDocumentation
{
    [ShellComponent]
    public class DocumentationProviderComponent
    {
        private static readonly ILogger OurLogger = Logger.GetLogger(typeof(DocumentationProviderComponent));
        public Dictionary<string, ReflectionDescription> documentation { get; }
        
        public DocumentationProviderComponent(ApplicationPackages applicationPackages,
            IDeployedPackagesExpandLocationResolver resolver)
        {
            var pathToDocumentation = GetPathToDocumentationFolder(applicationPackages, resolver);
            documentation = UEYamlParser.ParseDocs(pathToDocumentation.FullPath);
        }

        private static FileSystemPath GetPathToDocumentationFolder(ApplicationPackages applicationPackages, IDeployedPackagesExpandLocationResolver resolver)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var package = applicationPackages.FindPackageWithAssembly(assembly, OnError.LogException);
            var installDirectory = resolver.GetDeployedPackageDirectory(package);
            var editorPluginPathFile = installDirectory.Parent.Combine("documentation").Combine("yaml");
            return editorPluginPathFile;
        }
    }
}