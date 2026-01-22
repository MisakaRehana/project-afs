using ProjectAFS.Core.Abstracts.Services.Configuration;

namespace ProjectAFS.Core.Models.Configuration;

public sealed class AFSPathOptions : IPathOptions
{
	public string BasePath { get; }
	public string TempPath { get; }
	public string PluginPath { get; }
	public string PluginStagingPath { get; }
	public string ThemePath { get; }

	public AFSPathOptions(string basePath)
	{
		BasePath = basePath;
		TempPath = Path.Combine(Path.GetTempPath(), "Misaka Castle", "project-afs");
		PluginPath = Path.Combine(BasePath, "Plugins");
		PluginStagingPath = Path.Combine(TempPath, "PluginStaging");
		ThemePath = Path.Combine(BasePath, "Themes");
	}
}