using System.Runtime.Loader;
using ProjectAFS.Core.Internal.Plugins;

namespace ProjectAFS.Core.Abstractions.Plugins;

public interface IPluginManager
{
	// Lifetime Management
	Task LoadPluginsAsync(CancellationToken cancellationToken = default);
	Task UnloadPluginAsync(string pluginId, CancellationToken cancellationToken = default);
	
	// Discovery & Enumeration
	IEnumerable<PluginInfo> GetLoadedPlugins();
	IEnumerable<PluginContext> GetLoadedPluginsWithContext();
	IEnumerable<PluginInfo> DiscoverPlugins();
	PluginStatus GetPluginStatus(string pluginId);
	
	// Setup & Deployment
	Task ScheduleNewPluginInstallAsync(string pluginPackagePath, CancellationToken cancellationToken = default);
	Task UninstallPluginAsync(string pluginId, CancellationToken cancellationToken = default);
	
	// State Management
	Task EnablePluginAsync(string pluginId, CancellationToken cancellationToken = default);
	Task DisablePluginAsync(string pluginId, CancellationToken cancellationToken = default);
	
	// Dependency & Isolation
	bool ValidateDependencies(string pluginId, out IEnumerable<string> missingDependencies);
	AssemblyLoadContext GetPluginLoadContext(string pluginId);
	
	// Events
	event EventHandler<PluginEventArgs> PluginLoaded;
	event EventHandler<PluginEventArgs> PluginUnloaded;
	event EventHandler<PluginEventArgs> PluginInstallationScheduled;
}