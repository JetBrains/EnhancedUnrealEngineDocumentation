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

        // Build XmlDocument from YAML — native XmlDocHtmlPresenter renders it identically to UCLASS
        var xmlDoc = new XmlDocument();
        var member = xmlDoc.CreateElement("member");
        xmlDoc.AppendChild(member);

        if (!string.IsNullOrEmpty(d.documentation?.text))
        {
            var summary = xmlDoc.CreateElement("summary");
            AppendMarkdownAsXml(xmlDoc, summary, d.documentation.text);
            member.AppendChild(summary);
        }

        if (d.comment != null)
        {
            var remarks = xmlDoc.CreateElement("remarks");
            AppendMarkdownAsXml(xmlDoc, remarks, d.comment);
            member.AppendChild(remarks);
        }

        if (d.samples is { Count: > 0 })
        {
            var example = xmlDoc.CreateElement("example");
            foreach (var sample in d.samples)
            {
                var code = xmlDoc.CreateElement("code");
                code.InnerText = sample.TrimEnd();
                example.AppendChild(code);
            }
            member.AppendChild(example);
        }

        var xmlPresenter = Shell.Instance.GetComponent<XmlDocHtmlPresenter>();
        var formattedTitle = FormatTitle(d.name);

        var text = XmlDocHtmlUtil.BuildHtml(
            (builder, output) =>
            {
                output.Append(formattedTitle);

                // Render description/remarks/example exactly like native UCLASS documentation
                xmlPresenter.AppendBody(builder, output, member, null, null, presentationLanguage, XmlDocHtmlUtil.CrefManager);

                // UPROPERTY signature badge (after body content)
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
                    output.Append($"<dl class=\"headers\"><dt><code>{prefix}{body}{suffix}</code></dt></dl>");
                }

                // Images
                if (d.images != null)
                {
                    output.Append("<table width=\"550\"><tr><td>");
                    output.Append(d.images.Select(it => $"<img src=\"https://unreal-garden.com/{it}\">").Join(""));
                    output.Append("</td></tr></table>");
                }

                output.Append("<table width=\"550\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\"><tr><td></td></tr></table>");
            },
            XmlDocHtmlUtil.NavigationStyle.None, _theming);

        // Inline links and Full Documentation — appended after BuildHtml for native pill/button rendering
        AppendInlineLinks(text, "Related:",      d.related,      d.category);
        AppendInlineLinks(text, "Opposite:",     d.antonyms,     d.category);
        AppendInlineLinks(text, "Incompatible:", d.incompatible, d.category);
        AppendInlineLinks(text, "Implies:",      d.implies,      d.category);
        text.Append($"<br><a href=\"https://unreal-garden.com/docs/{d.category}/#{d.name.ToLower()}\">Full Documentation</a>");

        return new QuickDocTitleAndText(text, _documentation.name.NON_LOCALIZABLE());
    }

    // Convert backtick markdown to <c> elements for proper inline code rendering
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
                parent.AppendChild(doc.CreateTextNode(parts[i]));
            }
        }
    }

    private static void AppendInlineLinks(RichText output, string label, System.Collections.Generic.List<string> items, string category)
    {
        if (items is not { Count: > 0 }) return;
        output.Append($"{label} ");
        output.Append(items.Select(it =>
            $"<a href=\"https://unreal-garden.com/docs/{category}/#{it.ToLower()}\">{it}</a>").Join(", "));
        output.Append("<br>");
    }
}
