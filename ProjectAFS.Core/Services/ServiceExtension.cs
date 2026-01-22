using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstracts.Services.AFSChan;
using ProjectAFS.Core.Abstracts.Services.Configuration;
using ProjectAFS.Core.Abstracts.Services.Extensibility;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.Abstracts.Services.ResourceManagement;
using ProjectAFS.Core.Models.Configuration;
using ProjectAFS.Core.Services.AFSChan;
using ProjectAFS.Core.Services.Configuration;
using ProjectAFS.Core.Services.Extensibility;
using ProjectAFS.Core.Services.Globalization;
using ProjectAFS.Core.Services.ResourceManagement;
using ProjectAFS.Core.Services.Startup;

namespace ProjectAFS.Core.Services;

public static class ServiceExtension
{
	extension(IServiceCollection services)
	{
		public IServiceCollection AddAFSLogging()
		{
			services.AddLogging(config =>
			{
#if DEBUG
				config.AddConsole();
				config.SetMinimumLevel(LogLevel.Debug);
				config.AddDebug();
#else
				if (OperatingSystem.IsWindows())
				{
					config.AddConsole();
					config.SetMinimumLevel(LogLevel.Information);
					config.AddEventLog();
				}
				else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
				{
					config.AddConsole();
					config.SetMinimumLevel(LogLevel.Debug);
				}
				else if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
				{
					config.AddConsole(); // in Android platform this will redirect to logcat; in iOS it goes to system log
					config.SetMinimumLevel(LogLevel.Debug);
				}
#endif
			});
			return services;
		}
		
		public IServiceCollection AddAFSResManager()
		{
			services.AddSingleton<IAFSResManager, AFSResManager>();
			return services;
		}
		
		public IServiceCollection AddAFSConfiguration()
		{
			services.AddSingleton<IAFSConfiguration, AFSConfiguration>();
			services.AddSingleton<IConfigService, ConfigService>();
			services.AddSingleton<IPathOptions, AFSPathOptions>(_ => new AFSPathOptions(AppContext.BaseDirectory));
			return services.AddHostedService<ConfigService>(sp => (ConfigService)sp.GetRequiredService<IConfigService>());
		}
		
		public IServiceCollection AddAFSChan()
		{
			services.AddSingleton<IAFSChanService, AFSChanService>();
			return services.AddHostedService<AFSChanService>(sp => (AFSChanService)sp.GetRequiredService<IAFSChanService>());
		}
		
		public IServiceCollection AddI18n()
		{
			services.AddSingleton<II18nService, I18nService>();
			return services.AddHostedService<I18nService>(sp => (I18nService)sp.GetRequiredService<II18nService>());
		}

		public IServiceCollection AddStartupBootstrap()
		{
			// services.AddSingleton<StartupProgressProxy>(); // this is added in AFSApp class before all other services
			services.AddHostedService<StartupHostService>();
			return services;
		}
		
		public IServiceCollection AddPlugins()
		{
			services.AddSingleton<IPluginInstallerService, PluginInstallerService>();
			services.AddSingleton<IPluginService, PluginService>();
			return services.AddHostedService<PluginService>(sp => (PluginService)sp.GetRequiredService<IPluginService>());
		}
	}
}