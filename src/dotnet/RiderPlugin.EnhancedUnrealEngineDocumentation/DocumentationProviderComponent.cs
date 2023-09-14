using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Application;
using JetBrains.Application.Environment;
using JetBrains.Util;
using JetBrains.Util.Logging;
using YamlDotNet.Serialization;

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
            documentation = new Dictionary<string, ReflectionDescription>();
            var pathToDocumentation = GetPathToDocumentationFolder(applicationPackages, resolver);
            
            foreach (var enumerateFile in pathToDocumentation.GetChildFiles())
            {
                if (enumerateFile.NameWithoutExtension.Equals("args")) continue;
                
                var deserializer = new DeserializerBuilder().Build();
                using var reader = File.OpenText(enumerateFile.FullPath);
                try
                {
                    var reflectionDescriptions = deserializer.Deserialize<ReflectionDescriptions>(reader);
                    foreach (var reflectionDescriptionsSpecifier in reflectionDescriptions.specifiers)
                    {
                        documentation[reflectionDescriptionsSpecifier.name] = reflectionDescriptionsSpecifier;
                    }
                }
                catch (Exception e)
                {
                    OurLogger.Error(e, $"[EUED] Failed to parse {enumerateFile.FullPath}");
                }
            }
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