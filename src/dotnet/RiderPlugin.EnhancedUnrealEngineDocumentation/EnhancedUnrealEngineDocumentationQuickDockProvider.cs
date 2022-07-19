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
using JetBrains.ReSharperCpp.RiderPlugin.QuickDoc;

namespace RiderPlugin.EnhancedUnrealEngineDocumentation
{
    [QuickDocProvider(1)]
    public class EnhancedUnrealEngineDocumentationQuickDockProvider : RiderCppQuickDocProvider
    {
        public EnhancedUnrealEngineDocumentationQuickDockProvider(CppGlobalSymbolCache symbolCache, ICppUE4SolutionDetector ue4SolutionDetector, CppHighlighterColorCache colorCache, ITheming theming, CppDeclaredElementDescriptionProvider descriptionProvider, IPsiServices psiServices) : base(symbolCache, ue4SolutionDetector, colorCache, theming, descriptionProvider, psiServices)
        {
        }

        protected override CppUE4ReflectionSpecifiersQuickDocPresenterBase CreateUE4ReflectionSpecifiersQuickDocPresenter(CppUE4ReflectionSpecifiers.IItem item)
        {
            var documentationProviderComponent = Shell.Instance.GetComponent<DocumentationProviderComponent>();
            var newItem = item;
            if (documentationProviderComponent.Documentation.ContainsKey(newItem.Name))
            {
                var reflectionDescription = documentationProviderComponent.Documentation[newItem.Name];
                newItem = new CppUE4ReflectionSpecifiers.SimpleItem(item.Name, reflectionDescription.ToHTML());
            }
            return base.CreateUE4ReflectionSpecifiersQuickDocPresenter(newItem);
        }
    }
}