using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstractions.Localization;
using ProjectAFS.Core.Abstractions.Plugins;
using ProjectAFS.Core.Internal.Configuration;
using ProjectAFS.Core.Internal.I18n;
using ProjectAFS.Core.Internal.Plugins;

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
		services.AddSingleton<II18nManager, I18nManager>();
		services.AddSingleton<IPluginInstaller, PluginInstaller>();
		services.AddSingleton<IPluginManager, PluginManager>();
		services.AddAFSApp();
	}
}