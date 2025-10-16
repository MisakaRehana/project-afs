namespace ProjectAFS.Core.Internal.Plugins;

public class PluginEventArgs : EventArgs
{
	public string PluginId { get; }
	public PluginStatus Status { get; }

	public PluginEventArgs(string pluginId, PluginStatus status)
	{
		PluginId = pluginId;
		Status = status;
	}
}