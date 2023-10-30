using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.UI.Components.Theming;
using JetBrains.ReSharper.Feature.Services.Cpp.QuickDoc;
using JetBrains.ReSharper.Feature.Services.Cpp.UE4;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Lang;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.UE4;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace RiderPlugin.EnhancedUnrealEngineDocumentation;

[QuickDocProvider(-1)]
public class EnhancedUnrealEngineDocumentationQuickDockProvider : CppQuickDocProvider
{
    private readonly ICppCrefManagerProvider _crefManagerProvider;
    private static readonly ILogger OurLogger = Logger.GetLogger(typeof(EnhancedUnrealEngineDocumentationQuickDockProvider));
    public EnhancedUnrealEngineDocumentationQuickDockProvider(CppGlobalSymbolCache symbolCache, ICppUE4SolutionDetector ue4SolutionDetector, CppHighlighterColorCache colorCache, ITheming theming, CppDeclaredElementDescriptionProvider descriptionProvider, IPsiServices psiServices, IEnumerable<CppDeclaredElementOnlineHelpProvider> onlineHelpProviders, ICppCrefManagerProvider crefManagerProvider) : base(symbolCache, ue4SolutionDetector, colorCache, theming, descriptionProvider, psiServices, onlineHelpProviders, crefManagerProvider)
    {
        _crefManagerProvider = crefManagerProvider;
    }

    [CanBeNull]
    private EnhancedReflectionQuickDocPresenter CreateEnhancedPresenter(string name)
    {
        var documentationProviderComponent = Shell.Instance.TryGetComponent<DocumentationProviderComponent>();

        if (documentationProviderComponent == null)
        {
            OurLogger.Warn("[EUED] Couldn't retrive DocumentationProviderComponent component, falling back to default documentation");
            return null;
        }

        if (!documentationProviderComponent.documentation.TryGetValue(name, out var reflectionDescription)) return null;
        
        var presenter = CppXmlDocPresenterBase.Create(PsiServices, _crefManagerProvider.Create());
        return new EnhancedReflectionQuickDocPresenter(reflectionDescription, Theming, presenter, ColorCache);
    }

    protected override IQuickDocPresenter CreateUE4ReflectionSpecifiersQuickDocPresenter(CppUE4ReflectionSpecifiers.IItem item)
    {
        var enhancedDocPresenter = CreateEnhancedPresenter(item.Name);
        return enhancedDocPresenter ?? base.CreateUE4ReflectionSpecifiersQuickDocPresenter(item);
    }

    protected override IQuickDocPresenter CreateUE4MetadataSpecifiersQuickDocPresenter(
        CppResolveEntityDeclaredElement enumerator)
    {
        var enhancedDocPresenter = CreateEnhancedPresenter(enumerator.ShortName);
        return enhancedDocPresenter ?? base.CreateUE4MetadataSpecifiersQuickDocPresenter(enumerator);
    }
}