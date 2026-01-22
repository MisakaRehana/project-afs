using ProjectAFS.Core.Models.Extensibility;
using ProjectAFS.Core.Utility.Threading;

namespace ProjectAFS.Core.Abstracts.Services.Extensibility;

public interface IPluginService
{
	event EventHandler<PluginEventArgs>? PluginLoading;
	event EventHandler<PluginEventArgs>? PluginLoaded;
	event EventHandler<PluginEventArgs>? PluginUnloaded;
	event EventHandler<PluginEventArgs>? PluginInstallationScheduled;
	event EventHandler<PluginEventArgs>? PluginUninstallationScheduled;
	
	IEnumerable<AFSPluginInfo> DiscoverPlugins();
	AFSTask ExecutePendingOperationsAsync(CancellationToken cancellationToken = default);
	AFSTask LoadPluginsAsync(bool skipDiscovering = false, CancellationToken cancellationToken = default);
	AFSTask ScheduleNewPluginInstallAsync(string pluginPackagePath, CancellationToken cancellationToken = default);
	AFSTask SchedulePluginUninstallAsync(string pluginId, CancellationToken cancellationToken = default);
	void EnablePlugin(string pluginId);
	void DisablePlugin(string pluginId);
}