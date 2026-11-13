using System.Runtime.CompilerServices;
using Old8Lang.Hosting;
using Old8Lang.Modules.Official;

namespace Old8Lang.Tests;

internal static class ModuleRegistration
{
    [ModuleInitializer]
    internal static void RegisterOfficialModules()
    {
        var hostBuilder = new Old8HostBuilder();
        OfficialModules.RegisterAll(hostBuilder);
        hostBuilder.Build().InstallAsDefault();
    }
}
