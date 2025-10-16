using System.IO;
using Microsoft.Extensions.DependencyInjection;
using ProjectAFS.Core.Abstractions.Configuration;
using ProjectAFS.Extensibility.Abstractions.Configuration;

namespace ProjectAFS.Core.Internal.Configuration;

public class AFSPathOptions : IPathOptions, IPluginPathOptions
{
	public string BasePath { get; }
	public string TempPath { get; }
	public string I18nLanguagePath { get; }
	public string PluginPath { get; }
	public string PluginStagingPath { get; }
	public string ThemePath { get; }
	public string PluginManifestFileName => "plugins.json";
	public string PluginStateFileName => "plugins.lock.json";
	public string PluginPendingStateFileName => "plugins.pending.json";
	
	public AFSPathOptions(string basePath)
	{
		BasePath = basePath;
		TempPath = Path.Combine(Path.GetTempPath(), "Misaka Castle", "project-afs");
		I18nLanguagePath = Path.Combine(BasePath, "Languages");
		PluginPath = Path.Combine(BasePath, "Plugins");
		PluginStagingPath = Path.Combine(TempPath, "PluginStaging");
		ThemePath = Path.Combine(BasePath, "Themes");
	}
}

public static class AFSPathOptionsExtensions
{
	public static IServiceCollection AddAFSPathOptions(this IServiceCollection services, string basePath)
	{
		return services.AddSingleton<IPathOptions>(new AFSPathOptions(basePath))
			.AddSingleton<IPluginPathOptions>(sp => (IPluginPathOptions)sp.GetRequiredService<IPathOptions>());
	}
}