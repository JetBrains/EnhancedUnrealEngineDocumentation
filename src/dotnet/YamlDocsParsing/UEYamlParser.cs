using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Util;
using JetBrains.Util.Logging;
using YamlDotNet.Serialization;

namespace YamlDocsParsing
{
    public static class UEYamlParser
    {
        private static readonly ILogger OurLogger = Logger.GetLogger(typeof(UEYamlParser));
        public static Dictionary<string, ReflectionDescription> ParseDocs(string pathToFolder, bool throwOnError = false)
        {
            var documentation = new Dictionary<string, ReflectionDescription>();
            var pathToDocumentation = FileSystemPath.Parse(pathToFolder);
            
            foreach (var enumerateFile in pathToDocumentation.GetChildFiles())
            {
                if (enumerateFile.NameWithoutExtension.Equals("args")) continue;
                var fileName = enumerateFile.NameWithoutExtension;

                var parsedDocs = ParseDoc(enumerateFile, throwOnError);
                foreach (var keyValuePair in parsedDocs)
                {
                    var key = $"{fileName}_{keyValuePair.Key}";
                    documentation[key] = keyValuePair.Value;
                }
            }
            return documentation;
        }

        private static Dictionary<string, ReflectionDescription> ParseDoc(FileSystemPath pathToFile, bool throwOnError)
        {
            var documentation = new Dictionary<string, ReflectionDescription>();
            var deserializer = new DeserializerBuilder().Build();
            using (var reader = File.OpenText(pathToFile.FullPath))
            {
                try
                {
                    var reflectionDescriptions = deserializer.Deserialize<ReflectionDescriptions>(reader);
                    foreach (var reflectionDescriptionsSpecifier in reflectionDescriptions.specifiers)
                    {
                        reflectionDescriptionsSpecifier.category = pathToFile.NameWithoutExtension;
                        documentation[reflectionDescriptionsSpecifier.name] = reflectionDescriptionsSpecifier;
                    }
                }
                catch (ArgumentException e)
                {
                    OurLogger.Error(e, $"[EUED] Duplicate key in {pathToFile.FullPath}: {e.Message}");
                    if (throwOnError) throw;
                }
                catch (YamlDotNet.Core.YamlException e)
                {
                    OurLogger.Error(e, $"[EUED] YAML Exception in {pathToFile.FullPath}: {e.Message}");
                    if (throwOnError) throw;
                }
                catch (Exception e)
                {
                    OurLogger.Error(e, $"[EUED] Failed to parse {pathToFile.FullPath}");
                    if (throwOnError) throw;
                }

                return documentation;
            }
        }
    }
}