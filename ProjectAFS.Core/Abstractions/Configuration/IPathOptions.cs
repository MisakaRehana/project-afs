using Newtonsoft.Json;
using ProjectAFS.Extensibility.Abstractions.Configuration;

namespace ProjectAFS.Core.Abstractions.Configuration;

public interface IPathOptions
{
	string BasePath { get; }
	string TempPath { get; }
	string I18nLanguagePath { get; }
	string PluginPath { get; }
	string PluginStagingPath { get; }
	string ThemePath { get; }
	string PluginManifestFileName { get; }
	string PluginStateFileName { get; }
	string PluginPendingStateFileName { get; }
}