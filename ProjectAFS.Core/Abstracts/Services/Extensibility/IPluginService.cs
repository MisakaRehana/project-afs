using ProjectAFS.Core.Abstracts.Projects;
using ProjectAFS.Core.Models.Extensibility;
using ProjectAFS.Core.Services.Extensibility;
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
	void RegisterBuiltInPlugin<TPlugin>() where TPlugin : class, IPlugin, new();
	IEnumerable<PluginContext> GetLoadedPlugins();
	AFSTask ScheduleNewPluginInstallAsync(string pluginPackagePath, CancellationToken cancellationToken = default);
	AFSTask SchedulePluginUninstallAsync(string pluginId, CancellationToken cancellationToken = default);
	void EnablePlugin(string pluginId);
	void DisablePlugin(string pluginId);
	
	IEnumerable<IProjectTemplate> GetProjectTemplatesFromPlugins();
	void RegisterProjectTemplateProvider(IProjectTemplateProvider provider);
}