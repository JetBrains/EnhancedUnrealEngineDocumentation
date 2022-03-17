using System.Collections.Generic;

namespace RiderPlugin.EnhancedUnrealEngineDocumentation
{
    public static class DocumentationData
    {
        public static readonly Dictionary<string, string> DOCS = new()
        {
            {
                "VisibleAnywhere",
                @"
  name: VisibleAnywhere
  group: Editor
  subgroup: Visibility
  position: main
  type: flag
  incompatible: [ VisibleDefaultsOnly, VisibleInstanceOnly, EditAnywhere, EditDefaultsOnly, EditInstanceOnly ]
  comment:
    Properties marked with `VisibleAnywhere` are visible in the both Details Panel of Blueprint assets and the Details Panel of Blueprint instances within maps.
    
    Note that this refers to being visible in the *Details Panel*, not visible in the *Blueprint Graph*. For that you need to use `BlueprintReadOnly`.
  sample: |
    UPROPERTY(VisibleAnywhere)
    int32 VisibleAnywhereNumber;
  documentation:
    text: Indicates that this property is visible in all property windows, but cannot be edited. This Specifier is incompatible with the 'Edit' Specifiers.
    source: https://docs.unrealengine.com/4.27/en-US/ProgrammingAndScripting/GameplayArchitecture/Properties/Specifiers/
    images: [ /assets/unreal/uproperty/visibility-defaults-selected.png, /assets/unreal/uproperty/visibility-instance-selected.jpg ]
"
            }
        };
    }
}