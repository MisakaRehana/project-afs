using ProjectAFS.Core.Models.Extensibility;
using ProjectAFS.Core.Utility.Threading;

namespace ProjectAFS.Core.Abstracts.Services.Extensibility;

public interface IPluginInstallerService
{
	event EventHandler<InstallerOperation>? ExecutingOperation;

	AFSTask ExecutePendingOperationsAsync<T>(IProgress<T> progress, CancellationToken cancellationToken = default);
	IAsyncEnumerable<InstallerOperation> GetPendingInstallationsAsync(CancellationToken cancellationToken = default);
	IAsyncEnumerable<InstallerOperation> GetPendingUninstallationsAsync(CancellationToken cancellationToken = default);
	AFSTask ScheduleInstallAsync(string srcPackagePath, string dstPackagePath, CancellationToken cancellationToken = default);
	AFSTask ScheduleUninstallAsync(string pluginId, string pluginPackagePath, CancellationToken cancellationToken = default);
}