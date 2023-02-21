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

        string markdown_to_html(string markdown)
        {
            // TODO: Parse markdown
            return markdown;
        }

        // TODO: Look for markdown=>HTML parser?
        // TODO: Download images locally?
        // TODO: other tags
        public string ToHTML()
        {
            string doc = $"";

            if (documentation.text.IsNotEmpty())
            {
                doc += markdown_to_html(documentation.text);
            }

            if (comment.IsNotEmpty())
            {
                doc += markdown_to_html(comment);
            }

            if (position.IsNotEmpty() && type.IsNotEmpty())
            {
                string attribute = "UPROPERTY";
                string prefix = position == "main" ? $"{attribute}(" : $"{attribute}(meta=(";
                string suffix = position == "main" ? ")" : "))";
                string body = "";

                if (type == "flag")
                {
                    body = $"{name}";
                }
                else if (type == "bool")
                {
                    body = $"{name}=true";
                }
                else if (type == "string")
                {
                    body = $"{name}=\"abc\"";
                }
                else if (type == "number" || type =="integer")
                {
                    body = $"{name}=123";
                }

                doc += $"<p><code>{prefix}{body}{suffix}</code</p>";
            }

            if (samples != null && samples.Capacity > 0)
            {
                doc += $"<h4>Samples:</h4>" +
                       samples.Select(it => $"<pre>{it}</pre><hr/>").Join("");
            }

            doc += $"<dl>";
            if (related != null && related.Capacity > 0)
            {
                doc += $"<dt>Related:</dt>" +
                       related.Select(it =>
                               $"<dd><a href=\"https://benui.ca/unreal/uproperty/#{it.ToLower()}\">{it}</a></dd>")
                           .Join("");
            }
            if (antonyms != null && antonyms.Capacity > 0)
            {
                doc += $"<dt>Opposite:</dt>" +
                       antonyms.Select(it =>
                               $"<dd><a href=\"https://benui.ca/unreal/uproperty/#{it.ToLower()}\">{it}</a></dd>")
                           .Join("");
            }
            if (incompatible != null && incompatible.Capacity > 0)
            {
                doc += $"<dt>Incompatible:</dt>" +
                       incompatible.Select(it =>
                               $"<dd><a href=\"https://benui.ca/unreal/uproperty/#{it.ToLower()}\">{it}</a></dd>")
                           .Join("");
            }
            if (implies != null && implies.Capacity > 0)
            {
                doc += $"<dt>Implies:</dt>" +
                       implies.Select(it =>
                               $"<dd><a href=\"https://benui.ca/unreal/uproperty/#{it.ToLower()}\">{it}</a></dd>")
                           .Join("");
            }
            doc += $"</dl>";


            //doc += $"<a href=\"https://benui.ca/unreal/uproperty/#{name.ToLower()}\">Full Documentation</a><br>";
            //doc += documentation.images.Select(it => $"<img src=\"https://benui.ca/{it}\">").Join("");

            return doc;
        }
    }
    

    public class Documentation
    {
        public string text { get; set; }
        public string source { get; set; }
    }
}