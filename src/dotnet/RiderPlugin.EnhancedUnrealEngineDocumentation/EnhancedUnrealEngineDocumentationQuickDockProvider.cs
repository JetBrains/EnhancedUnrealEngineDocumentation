using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.UI.Components.Theming;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.QuickDoc;
using JetBrains.ReSharper.Feature.Services.Cpp.UE4;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Lang;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.Cpp.UE4;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.TextControl.CodeWithMe;
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

        var macroName = GetMacroName();
        var key = $"{macroName}_{name}";

        if (!documentationProviderComponent.documentation.TryGetValue(key, out var reflectionDescription)) return null;
        
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

    private string GetMacroName()
    {
        // 1. Get the current text control (editor) from the data context or manager
        var documentEditorManager = PsiServices.Solution.GetComponent<ITextControlManager>();
        var textControl = documentEditorManager.LastFocusedTextControlPerClient.ForCurrentClient();


        if (textControl == null) return "";
        // 2. Look for the CppUE4Metadata element at the caret position
        // We use GetElements to find the specific UE4 Metadata block the caret is in
        var metadata = TextControlToPsi.GetElements<CppUE4Metadata>(PsiServices.Solution, textControl.Caret.DocumentOffset()).FirstOrDefault();

        if (metadata == null) return "";
            
        var macroType = metadata.GetMacroType();
        return MacroTypeToString(macroType);
    }

    private static string MacroTypeToString(UE4ReflectionMacroType macroType)
    {
        switch (macroType)
        {
            case UE4ReflectionMacroType.Class:
                return "uclass";
            case UE4ReflectionMacroType.Function:
                return "ufunction";
            case UE4ReflectionMacroType.Delegate:
                return ""; 
            case UE4ReflectionMacroType.Property:
                return "uproperty";
            case UE4ReflectionMacroType.Struct:
                return "ustruct";
            case UE4ReflectionMacroType.Enum:
                return "uenum";
            case UE4ReflectionMacroType.Meta:
                return "umeta";
            case UE4ReflectionMacroType.Param:
                return "uparam";
            case UE4ReflectionMacroType.Interface:
                return "uinterface";
            case UE4ReflectionMacroType.None:
            case UE4ReflectionMacroType.GeneratedBody:
            case UE4ReflectionMacroType.GeneratedBodyLegacy:
            case UE4ReflectionMacroType.RigVMMethod:
            default:
                return "";
        }
    }
}