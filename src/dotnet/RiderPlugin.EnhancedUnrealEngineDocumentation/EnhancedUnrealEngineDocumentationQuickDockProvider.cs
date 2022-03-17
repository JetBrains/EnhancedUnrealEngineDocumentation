using JetBrains.Application.UI.Components.Theming;
using JetBrains.ReSharper.Feature.Services.Cpp.QuickDoc;
using JetBrains.ReSharper.Feature.Services.Cpp.UE4;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Lang;
using JetBrains.ReSharper.Psi.Cpp.UE4;
using JetBrains.ReSharperCpp.RiderPlugin.QuickDoc;
using YamlDotNet.Serialization;

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
            var newItem = item;
            if (DocumentationData.DOCS.ContainsKey(newItem.Name))
            {
                var yamlDescription = DocumentationData.DOCS[newItem.Name];
                var deserializer = new DeserializerBuilder().Build();
                var reflectionDescription = deserializer.Deserialize<ReflectionDescription>(yamlDescription);
                newItem = new CppUE4ReflectionSpecifiers.SimpleItem(item.Name, reflectionDescription.ToHTML());
            }
            return base.CreateUE4ReflectionSpecifiersQuickDocPresenter(newItem);
        }
    }
}