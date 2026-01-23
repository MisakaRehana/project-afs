using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectAFS.Core.Services;

namespace ProjectAFS.Core;

public static class Startup
{
	public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
	{
		services.AddAFSLogging();
		services.AddAFSResManager();
		services.AddAFSConfiguration(); // this will load settings (allow I18nService to fetch preferred language)
		services.AddAFSChan(); // must add before I18n to allow AFSChan to localize its messages by AOP patching
		services.AddI18n(); // this will enable globalization support (all services after this are able to display localized splash loading messages)
		services.AddStartupBootstrap(); // this will awake splash screen
		services.AddPlugins(); // first plugin installer service, then plugin service itself (this will exec pending plugin operations, then load plugins) [UI Notificable]
		services.Complete();
	}
}