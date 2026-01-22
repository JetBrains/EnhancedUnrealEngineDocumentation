using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Psi;
using JetBrains.Rider.Backend.Env;
using JetBrains.Rider.Backend.Product;

namespace RiderPlugin.EnhancedUnrealEngineDocumentation
{
    [ZoneMarker]
    public class ZoneMarker :
        IRequire<ILanguageCppZone>,
        IRequire<IRiderFeatureZone>,
        IRequire<IRiderProductEnvironmentZone>
    {
    }
}