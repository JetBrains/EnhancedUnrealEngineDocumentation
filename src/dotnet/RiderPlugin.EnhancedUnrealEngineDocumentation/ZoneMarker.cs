using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.Rider.Backend.Env;
using JetBrains.Rider.Backend.Product;

namespace RiderPlugin.EnhancedUnrealEngineDocumentation
{
    [ZoneMarker]
    public class ZoneMarker :
        IRequire<ILanguageCppZone>,
        IRequire<DaemonZone>,
        IRequire<IRiderFeatureZone>,
        IRequire<IRiderProductEnvironmentZone>
    {
    }
}