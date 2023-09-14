using System.Collections.Generic;
using JetBrains.Application.UI.Components.Theming;
using JetBrains.ReSharper.Feature.Services.Cpp.QuickDoc;
using JetBrains.ReSharper.Feature.Services.Cpp.UE4;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Lang;
using JetBrains.ReSharper.Psi.Cpp.UE4;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace RiderPlugin.EnhancedUnrealEngineDocumentation;

[QuickDocProvider(1)]
public class EnhancedUnrealEngineDocumentationQuickDockProvider : CppQuickDocProvider
{
    private static readonly ILogger OurLogger = Logger.GetLogger(typeof(EnhancedUnrealEngineDocumentationQuickDockProvider));
    public EnhancedUnrealEngineDocumentationQuickDockProvider(CppGlobalSymbolCache symbolCache, ICppUE4SolutionDetector ue4SolutionDetector, CppHighlighterColorCache colorCache, ITheming theming, CppDeclaredElementDescriptionProvider descriptionProvider, IPsiServices psiServices, IEnumerable<CppDeclaredElementOnlineHelpProvider> onlineHelpProviders, ICppCrefManagerProvider crefManagerProvider) : base(symbolCache, ue4SolutionDetector, colorCache, theming, descriptionProvider, psiServices, onlineHelpProviders, crefManagerProvider)
    {
    }

    protected override CppUE4ReflectionSpecifiersQuickDocPresenter CreateUE4ReflectionSpecifiersQuickDocPresenter(CppUE4ReflectionSpecifiers.IItem item)
    {
        var documentationProviderComponent = Shell.Instance.TryGetComponent<DocumentationProviderComponent>();
        var newItem = item;
        if (documentationProviderComponent != null && documentationProviderComponent.documentation.TryGetValue(newItem.Name, out var reflectionDescription))
        {
            newItem = new CppUE4ReflectionSpecifiers.SimpleItem(item.Name, reflectionDescription.ToHTML());
        }

        if (documentationProviderComponent == null)
        {
            OurLogger.Warn("[EUED] Couldn't retrive DocumentationProviderComponent component, falling back to default documentation");
        }
        return base.CreateUE4ReflectionSpecifiersQuickDocPresenter(newItem);
    }
}