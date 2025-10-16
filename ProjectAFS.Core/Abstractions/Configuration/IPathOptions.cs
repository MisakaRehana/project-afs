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

public interface IAFSConfiguration
{
	Dictionary<string, object> AppSettings { get; }

	T GetValue<T>(string key);
	
	object this[string key] { get; set; }
}