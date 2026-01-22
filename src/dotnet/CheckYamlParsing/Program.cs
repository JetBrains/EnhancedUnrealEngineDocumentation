using System;
using System.IO;
using YamlDocsParsing;

var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
var yamlPath = Path.Combine(solutionRoot, "documentation", "yaml");

Console.WriteLine($"Parsing YAML files from: {yamlPath}");

try
{
    var documentation = UEYamlParser.ParseDocs(yamlPath, true);
    Console.WriteLine($"Successfully parsed {documentation.Count} reflection descriptions.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error parsing YAML files: {ex.Message}");
    Environment.Exit(1);
}