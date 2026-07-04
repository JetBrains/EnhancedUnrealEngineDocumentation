using System.Linq;
using System.Xml;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.Feature.Services.Cpp.QuickDoc;
using JetBrains.ReSharper.Feature.Services.Cpp.Resources;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Render;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Lang;
using JetBrains.Application.I18n;
using JetBrains.Application.UI.Components.Theming;
using JetBrains.ReSharper.Psi.Cpp.Presentation;
using JetBrains.UI.RichText;
using JetBrains.Util;
using JetBrains.Util.RichText;
using YamlDocsParsing;

namespace RiderPlugin.EnhancedUnrealEngineDocumentation;

public class EnhancedReflectionQuickDocPresenter : CppUE4SpecifiersQuickDocPresenter
{
    private readonly ReflectionDescription _documentation;
    private readonly ITheming _theming;

    public EnhancedReflectionQuickDocPresenter(ReflectionDescription documentation, ITheming theming, CppXmlDocPresenterBase presenter, CppHighlighterColorCache colorCache) : base(presenter, colorCache)
    {
        _documentation = documentation;
        _theming = theming;
    }

    private RichText FormatTitle(string name)
    {
        var specifier = Strings._ReflectionSpecifier_Text.Format("Unreal Engine");
        return Select(name, CppHighlightingAttributeIds.CPP_UE4_REFLECTION_SPECIFIER_NAME_ATTRIBUTE)
            .Append($"<br/>({specifier})", TextStyle.Default);
    }

    public override QuickDocTitleAndText GetHtml(PsiLanguageType presentationLanguage)
    {
        var d = _documentation;

        // Summary + Remarks in one XML document
        var mainDoc = new XmlDocument();
        var mainMember = mainDoc.CreateElement("member");
        mainDoc.AppendChild(mainMember);

        if (!string.IsNullOrEmpty(d.documentation?.text))
        {
            var summary = mainDoc.CreateElement("summary");
            AppendMarkdownAsXml(mainDoc, summary, d.documentation.text);
            mainMember.AppendChild(summary);
        }

        var hasLinks = (d.related?.Count > 0) || (d.antonyms?.Count > 0) || (d.incompatible?.Count > 0) || (d.implies?.Count > 0);
        if (d.comment != null || hasLinks)
        {
            var remarks = mainDoc.CreateElement("remarks");
            if (d.comment != null)
                AppendMarkdownAsXml(mainDoc, remarks, d.comment);
            AppendLinkGroupToXml(mainDoc, remarks, "Related:",      d.related,      d.category);
            AppendLinkGroupToXml(mainDoc, remarks, "Opposite:",     d.antonyms,     d.category);
            AppendLinkGroupToXml(mainDoc, remarks, "Incompatible:", d.incompatible, d.category);
            AppendLinkGroupToXml(mainDoc, remarks, "Implies:",      d.implies,      d.category);
            mainMember.AppendChild(remarks);
        }

        // Examples in a separate XML document so links can be inserted between remarks and example
        XmlElement exampleMember = null;
        if (d.samples is { Count: > 0 })
        {
            var exampleDoc = new XmlDocument();
            exampleMember = exampleDoc.CreateElement("member");
            exampleDoc.AppendChild(exampleMember);
            var example = exampleDoc.CreateElement("example");
            foreach (var sample in d.samples)
            {
                var code = exampleDoc.CreateElement("code");
                code.InnerText = sample.TrimEnd();
                example.AppendChild(code);
            }
            exampleMember.AppendChild(example);
        }

        var xmlPresenter = Shell.Instance.GetComponent<XmlDocHtmlPresenter>();
        var formattedTitle = FormatTitle(d.name);

        var text = XmlDocHtmlUtil.BuildHtml(
            (builder, output) =>
            {
                output.Append(formattedTitle);
                output.Append("<hr/>");

                // Signature badge right under the divider
                if (d.position.IsNotEmpty() && d.type.IsNotEmpty())
                {
                    var attribute = d.category?.ToUpper() ?? "UPROPERTY";
                    var prefix = d.position == "main" ? $"{attribute}(" : $"{attribute}(meta=(";
                    var suffix = d.position == "main" ? ")" : "))";
                    var body = d.type switch
                    {
                        "flag"                => d.name,
                        "bool"                => $"{d.name}=true",
                        "string"              => $"{d.name}=\"abc\"",
                        "number" or "integer" => $"{d.name}=123",
                        _                     => ""
                    };
                    output.Append($"<p><code>{prefix}{body}{suffix}</code></p>");
                }

                // Summary + Remarks (links are inside the remarks XML element)
                xmlPresenter.AppendBody(builder, output, mainMember, null, null, presentationLanguage, XmlDocHtmlUtil.CrefManager);

                // Example
                if (exampleMember != null)
                    xmlPresenter.AppendBody(builder, output, exampleMember, null, null, presentationLanguage, XmlDocHtmlUtil.CrefManager);

                // Images
                if (d.images != null)
                {
                    output.Append("<table width=\"550\"><tr><td>");
                    output.Append(d.images.Select(it => $"<img src=\"https://unreal-garden.com/{it}\">").Join(""));
                    output.Append("</td></tr></table>");
                }

                output.Append($"<br><a href=\"https://unreal-garden.com/docs/{d.category}/#{d.name.ToLower()}\">Full Documentation</a>");
                output.Append("<table width=\"550\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\"><tr><td></td></tr></table>");
            },
            XmlDocHtmlUtil.NavigationStyle.None, _theming);

        return new QuickDocTitleAndText(text, _documentation.name.NON_LOCALIZABLE());
    }

    private static void AppendMarkdownAsXml(XmlDocument doc, XmlElement parent, string text)
    {
        var parts = text.Split('`');
        for (var i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrEmpty(parts[i])) continue;
            if (i % 2 == 1)
            {
                var c = doc.CreateElement("c");
                c.InnerText = parts[i];
                parent.AppendChild(c);
            }
            else
            {
                AppendTextWithMarkdownLinks(doc, parent, parts[i]);
            }
        }
    }

    private static void AppendTextWithMarkdownLinks(XmlDocument doc, XmlElement parent, string text)
    {
        var remaining = text;
        while (remaining.Length > 0)
        {
            var linkStart = remaining.IndexOf('[');
            if (linkStart < 0) { parent.AppendChild(doc.CreateTextNode(remaining)); break; }

            if (linkStart > 0)
                parent.AppendChild(doc.CreateTextNode(remaining.Substring(0, linkStart)));

            var textEnd = remaining.IndexOf(']', linkStart);
            if (textEnd < 0 || textEnd + 1 >= remaining.Length || remaining[textEnd + 1] != '(')
            {
                parent.AppendChild(doc.CreateTextNode(remaining.Substring(linkStart)));
                break;
            }

            var urlStart = textEnd + 2;
            var urlEnd = remaining.IndexOf(')', urlStart);
            if (urlEnd < 0) { parent.AppendChild(doc.CreateTextNode(remaining.Substring(linkStart))); break; }

            var linkText = remaining.Substring(linkStart + 1, textEnd - linkStart - 1).Trim();
            var url = remaining.Substring(urlStart, urlEnd - urlStart).Trim();

            if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(linkText))
            {
                var see = doc.CreateElement("see");
                see.SetAttribute("href", url);
                see.AppendChild(doc.CreateTextNode(linkText));
                parent.AppendChild(see);
            }
            // [](url) — empty-text links are invisible in markdown, skip them

            remaining = remaining.Substring(urlEnd + 1);
        }
    }

    private static void AppendLinkGroupToXml(XmlDocument doc, XmlElement parent, string label, System.Collections.Generic.List<string> items, string category)
    {
        if (items is not { Count: > 0 }) return;
        var para = doc.CreateElement("para");
        para.AppendChild(doc.CreateTextNode($"{label} "));
        for (var i = 0; i < items.Count; i++)
        {
            var see = doc.CreateElement("see");
            see.SetAttribute("href", $"https://unreal-garden.com/docs/{category}/#{items[i].ToLower()}");
            see.AppendChild(doc.CreateTextNode(items[i]));
            para.AppendChild(see);
            if (i < items.Count - 1)
                para.AppendChild(doc.CreateTextNode(", "));
        }
//        parent.AppendChild(para);
    }
}
