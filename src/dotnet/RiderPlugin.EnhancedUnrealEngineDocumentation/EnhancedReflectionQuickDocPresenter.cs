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
        var doc = RichTextFromSimpleMarkdown.Convert(_documentation.documentation.text);

        var formattedTitle = FormatTitle(_documentation.name);
        var text = XmlDocHtmlUtil.BuildHtml(
            (_, output) => output.Append(formattedTitle).Append("<br><br>", TextStyle.Default).Append(doc),
            XmlDocHtmlUtil.NavigationStyle.None, _theming).Append("<br><br>");

        if (_documentation.comment != null)
        {
            var comment = RichTextFromSimpleMarkdown.Convert(_documentation.comment);
            text.Append("<blockquote><p>").Append(comment).Append("</p></blockquote>");
            text.Append("<br>");
        }
        if (_documentation.position.IsNotEmpty() && _documentation.type.IsNotEmpty())
        {
            var attribute = "UPROPERTY";
            var prefix = _documentation.position == "main" ? $"{attribute}(" : $"{attribute}(meta=(";
            var suffix = _documentation.position == "main" ? ")" : "))";

            var body = _documentation.type switch
            {
                "flag" => $"{_documentation.name}",
                "bool" => $"{_documentation.name}=true",
                "string" => $"{_documentation.name}=\"abc\"",
                "number" or "integer" => $"{_documentation.name}=123",
                _ => ""
            };

            text.Append($"<p><code>{prefix}{body}{suffix}</code</p>").Append("<br>");
            
        }
        if (_documentation.samples is { Capacity: > 0 })
        {
           text.Append($"<h4>Samples:</h4>" +
                       _documentation.samples.Select(it => $"<pre>{it}</pre><hr/>").Join(""));
           text.Append("<br>");
        }

        text.Append("<dl>");
        
        if (_documentation.related is { Capacity: > 0 })
        {
            text.Append($"<dt>Related:</dt><ul>");
            text.Append(_documentation.related.Select(it =>
                    $"<li><dd><a href=\"https://benui.ca/unreal/uproperty/#{it.ToLower()}\">{it}</a></dd></li>")
                .Join(""));
            text.Append("</ul>");
            text.Append("<br>");
        }
        if (_documentation.antonyms is { Capacity: > 0 })
        {
            text.Append($"<dt>Opposite:</dt><ul>");
            text.Append(_documentation.antonyms.Select(it =>
                    $"<li><dd><a href=\"https://benui.ca/unreal/uproperty/#{it.ToLower()}\">{it}</a></dd></li>")
                .Join(""));
            text.Append("</ul>");
            text.Append("<br>");
        }
        if (_documentation.incompatible is { Capacity: > 0 })
        {
            text.Append($"<dt>Incompatible:</dt><ul>");
            text.Append(_documentation.incompatible.Select(it =>
                    $"<li><dd><a href=\"https://benui.ca/unreal/uproperty/#{it.ToLower()}\">{it}</a></dd></li>")
                .Join(""));
            text.Append("</ul>");
            text.Append("<br>");
        }
        if (_documentation.implies is { Capacity: > 0 })
        {
            text.Append($"<dt>Implies:</dt><ul>");
            text.Append(_documentation.implies.Select(it =>
                    $"<li><dd><a href=\"https://benui.ca/unreal/uproperty/#{it.ToLower()}\">{it}</a></dd></li>")
                .Join(""));
            text.Append("</ul>");
            text.Append("<br>");
        }
        text.Append($"</dl>");
        
        text.Append($"<a href=\"https://benui.ca/unreal/uproperty/#{_documentation.name.ToLower()}\">Full Documentation</a><br>");
        if (_documentation.images != null)
        {
            text.Append(_documentation.images.Select(it => $"<img src=\"https://benui.ca/{it}\">").Join(""));
        }

        return new QuickDocTitleAndText(text, _documentation.name.NON_LOCALIZABLE());
    }
}