namespace ProjectAFS.Core.Abstracts.Services.Configuration;

public interface IPathOptions
{
	string BasePath { get; }
	string TempPath { get; }
	string PluginPath { get; }
	string PluginStagingPath { get; }
	string ThemePath { get; }
}