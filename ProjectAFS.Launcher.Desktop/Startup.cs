using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstractions.Configuration;
using ProjectAFS.Core.Abstractions.Localization;
using ProjectAFS.Core.Abstractions.Plugins;
using ProjectAFS.Core.Internal.Configuration;
using ProjectAFS.Core.Internal.I18n;
using ProjectAFS.Core.Internal.Plugins;
using ProjectAFS.Core.Utility.Services;
using ProjectAFS.Extensibility.Abstractions.Configuration;

namespace ProjectAFS.Launcher.Desktop;

public static class Startup
{
	public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
	{
		services.AddLogging(b =>
		{
#if LINUX
			b.AddSystemdConsole();
#else
			b.AddConsole();
#endif
#if DEBUG
			b.AddDebug();
#endif
		});
		services.AddAFSPathOptions(AppContext.BaseDirectory);
		services.AddDefaultImplementation<IAFSConfiguration>(); // ProjectAFS.Core.Internal.Configuration.AFSNativeConfiguration
		services.AddDefaultImplementation<II18nManager>();		// ProjectAFS.Core.Internal.Localization.I18nManager
		services.AddDefaultImplementation<IPluginInstaller>();	// ProjectAFS.Core.Internal.Plugins.PluginInstaller
		services.AddDefaultImplementation<IPluginManager>();	// ProjectAFS.Core.Internal.Plugins.PluginManager
		services.AddAFSApp(); // here we already added all App windows (see ProjectAFS.Launcher.Desktop.AFSApp.AddAFSAppWindows for more information)
	}
}