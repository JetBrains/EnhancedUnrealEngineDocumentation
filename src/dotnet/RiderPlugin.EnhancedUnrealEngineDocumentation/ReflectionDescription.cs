using System.Collections.Generic;
using System.Linq;
using JetBrains.Util;

namespace RiderPlugin.EnhancedUnrealEngineDocumentation
{
    public class ReflectionDescription
    {
        public string name { get; set; }
        public string group { get; set; }
        public string subgroup { get; set; }
        public string position { get; set; }
        public string type { get; set; }
        public List<string> incompatible { get; set; }
        public string comment { get; set; }
        public string sample { get; set; }
        public Documentation documentation { get; set; }

        public string ToHTML()
        {
            return $"{documentation.text}<br>" +
                   $"{comment}<br>" +
                   $"<b>{nameof(incompatible)}</b>:" +
                   $"<ul>" +
                   incompatible.Select(it => $"<li><a href=\"https://benui.ca/unreal/uproperty/#{it.ToLower()}\">{it}</a></li>").Join("") +
                   $"</ul>" +
                   $"<b>{nameof(group)}</b>: {group}<br>" +
                   $"<b>{nameof(subgroup)}</b>: {subgroup}<br>" +
                   $"<b>{nameof(position)}</b>: {position}<br>" +
                   $"<b>{nameof(type)}</b>: {type}<br>" +
                   $"<a href=\"https://benui.ca/unreal/uproperty/#{name.ToLower()}\">Full Documentation</a><br>" +
                   documentation.images.Select(it => $"<img src=\"https://benui.ca/{it}\">").Join("");
        }
    }

    public class Documentation
    {
        public string text { get; set; }
        public string source { get; set; }
        public List<string> images { get; set; }
    }
}