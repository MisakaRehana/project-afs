namespace ProjectAFS.Core.Internal.Plugins;

public enum PluginStatus
{
	Disabled = 0,
	Enabled = 1,
	Faulted = 2,
	Loading = 10,
	Unloading = 11
}