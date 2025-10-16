namespace ProjectAFS.Core.Abstractions.Plugins;

public interface IPluginInstaller
{
	Task ScheduleInstallAsync(string srcPackagePath, string dstPackagePath, CancellationToken cancellationToken = default);
	
	Task ScheduleUninstallAsync(string pluginId, string pluginPackagePath, CancellationToken cancellationToken = default);
	
	Task ExecutePendingOperationsAsync(CancellationToken cancellationToken = default); // only call when IDE starts/restarts
}