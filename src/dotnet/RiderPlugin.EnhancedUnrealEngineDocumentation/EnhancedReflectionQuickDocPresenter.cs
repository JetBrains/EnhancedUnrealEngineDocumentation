using System.Linq;
using JetBrains.ReSharper.Feature.Services.Cpp.QuickDoc;
using JetBrains.ReSharper.Feature.Services.Cpp.Resources;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Lang;
using JetBrains.Application.I18n;
using JetBrains.Application.UI.Components.Theming;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Render;
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
        var doc = RichTextFromSimpleMarkdown.Convert(_documentation.documentation?.text ?? string.Empty);
        var formattedTitle = FormatTitle(_documentation.name);
        var d = _documentation;

        var text = XmlDocHtmlUtil.BuildHtml(
            (_, output) => output.Append(formattedTitle).Append("<br><br>", TextStyle.Default).Append(doc),
            XmlDocHtmlUtil.NavigationStyle.None, _theming);

        // Everything below is appended after BuildHtml so IntelliJ renders h4/pre with native pill/box styling
        if (d.comment != null)
        {
            var comment = RichTextFromSimpleMarkdown.Convert(d.comment);
            text.Append("<blockquote><p>").Append(comment).Append("</p></blockquote>");
        }

        AppendInlineLinks(text, "Related:",      d.related,      d.category);
        AppendInlineLinks(text, "Opposite:",     d.antonyms,     d.category);
        AppendInlineLinks(text, "Incompatible:", d.incompatible, d.category);
        AppendInlineLinks(text, "Implies:",      d.implies,      d.category);

        text.Append($"<br><a href=\"https://unreal-garden.com/docs/{d.category}/#{d.name.ToLower()}\">Full Documentation</a>");

        if (d.position.IsNotEmpty() && d.type.IsNotEmpty())
        {
            var attribute = "UPROPERTY";
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
            text.Append($"<dl class=\"headers\"><dt><code>{prefix}{body}{suffix}</code></dt></dl>");
        }

        if (d.samples is { Capacity: > 0 })
        {
            text.Append("<p><code>Example:</code></p>");
            foreach (var sample in d.samples)
                text.Append($"<div class=\"code-block\"><pre>{AppendSyntaxColored(sample.TrimEnd())}</pre></div>");
        }

        if (d.images != null)
        {
            text.Append("<table width=\"550\"><tr><td>");
            text.Append(d.images.Select(it => $"<img src=\"https://unreal-garden.com/{it}\">").Join(""));
            text.Append("</td></tr></table>");
        }

        return new QuickDocTitleAndText(text, _documentation.name.NON_LOCALIZABLE());
    }

    private static void AppendInlineLinks(RichText output, string label, System.Collections.Generic.List<string> items, string category)
    {
        if (items is not { Count: > 0 }) return;
        output.Append($"{label} ");
        output.Append(items.Select(it =>
            $"<a href=\"https://unreal-garden.com/docs/{category}/#{it.ToLower()}\">{it}</a>").Join(", "));
        output.Append("<br>");
    }

    private static readonly System.Text.RegularExpressions.Regex KeywordRegex = new(
        @"\b(class|struct|public|private|protected|virtual|override|const|static|void|int32|int64|float|bool|true|false|return|UCLASS|UPROPERTY|UFUNCTION|USTRUCT|UENUM|UMETA|GENERATED_BODY|GENERATED_UCLASS_BODY)\b",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    private static string AppendSyntaxColored(string code)
    {
        var escaped = code
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

        // Color comments
        var lines = escaped.Split('\n');
        var result = new System.Text.StringBuilder();
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("//"))
                result.Append($"<span style=\"color:gray\">{line}</span>\n");
            else
                result.Append(KeywordRegex.Replace(line, "<b>$1</b>") + "\n");
        }
        return result.ToString().TrimEnd('\n');
    }
}