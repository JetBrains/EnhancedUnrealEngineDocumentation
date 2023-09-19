using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JetBrains.Util;
using YamlDotNet.Serialization;

namespace RiderPlugin.EnhancedUnrealEngineDocumentation
{
    public class ReflectionDescriptions
    {
        public List<ReflectionDescription> specifiers { get; set; }
    }
    public class ReflectionDescription
    {
        public string name { get; set; }
        public string group { get; set; }
        public string subgroup { get; set; }
        public string position { get; set; }
        public string version { get; set; }
        public string deprecated { get; set; }
        public string type { get; set; }
        public string source { get; set; }
        
        [YamlMember(Alias= "type-comment")]
        public string type_comment { get; set; }
        public List<string> required { get; set; }
        public List<string> requires { get; set; }
        public List<string> keywords { get; set; }
        public List<string> examples { get; set; }
        public List<string> synonyms { get; set; }
        public List<string> related { get; set; }
        public List<string> antonyms { get; set; }
        public bool inherited { get; set; }
        public List<string> incompatible { get; set; }
        public List<string> implies { get; set; }
        public string comment { get; set; }
        public List<string> samples { get; set; }
        public List<string> images { get; set; }
        public List<List<string>> links { get; set; }
        public Documentation documentation { get; set; }
    }
    

    public class Documentation
    {
        public string text { get; set; }
        public string source { get; set; }
    }
}