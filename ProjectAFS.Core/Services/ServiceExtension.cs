using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstracts.Services.Configuration;
using ProjectAFS.Core.Abstracts.Services.Extensibility;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.Services.Configuration;
using ProjectAFS.Core.Services.Extensibility;
using ProjectAFS.Core.Services.Globalization;

namespace ProjectAFS.Core.Services;

public static class ServiceExtension
{
	extension(IServiceCollection services)
	{
		public IServiceCollection AddAFSLogging()
		{
			services.AddLogging(config =>
			{
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
			});
			return services;
		}
		
		public IServiceCollection AddAFSConfiguration()
		{
			services.AddSingleton<IConfigService, ConfigService>();
			return services.AddHostedService<ConfigService>(sp => (ConfigService)sp.GetRequiredService<IConfigService>());
		}
		
		public IServiceCollection AddI18n()
		{
			services.AddSingleton<II18nService, I18nService>();
			return services.AddHostedService<I18nService>(sp => (I18nService)sp.GetRequiredService<II18nService>());
		}
		
		public IServiceCollection AddPlugins()
		{
			services.AddSingleton<IPluginService, PluginService>();
			return services.AddHostedService<PluginService>(sp => (PluginService)sp.GetRequiredService<IPluginService>());
		}
	}
}