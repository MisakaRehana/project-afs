using System.Runtime.CompilerServices;

namespace ProjectAFS.Core.Abstractions.Plugins;

public interface IPluginInstaller
{
	event EventHandler<InstallerOperation>? ExecutingOperation;
	
	Task ScheduleInstallAsync(string srcPackagePath, string dstPackagePath, CancellationToken cancellationToken = default);
	
	Task ScheduleUninstallAsync(string pluginId, string pluginPackagePath, CancellationToken cancellationToken = default);
	
	Task ExecutePendingOperationsAsync(CancellationToken cancellationToken = default); // only call when IDE starts/restarts
	IAsyncEnumerable<InstallerOperation> GetPendingInstallationsAsync(CancellationToken cancellationToken = default);
	IAsyncEnumerable<InstallerOperation> GetPendingUninstallationsAsync(CancellationToken cancellationToken = default);
}