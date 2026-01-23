using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProjectAFS.Core.Abstracts.Services.Configuration;
using ProjectAFS.Core.Abstracts.Services.Extensibility;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.Models.Extensibility;
using ProjectAFS.Core.Models.Startup;
using ProjectAFS.Core.Services.Startup;
using ProjectAFS.Core.Utility.SharpCompress;
using ProjectAFS.Core.Utility.Strings;
using ProjectAFS.Core.Utility.Threading;
using SharpCompress.Archives.Zip;

namespace ProjectAFS.Core.Services.Extensibility;

/// <summary>
/// Represents the service responsible for installing and uninstalling plugins.
/// </summary>
public sealed class PluginInstallerService : IPluginInstallerService // plugin installer is not a Hosted Service because it doesn't need to be integrated into HostedService lifecycle
{
	private const string ManifestFileName = "plugins.json";
	public event EventHandler<InstallerOperation>? ExecutingOperation;

	private readonly static SemaphoreSlim ManifestLock = new(1, 1);
	private readonly ILogger<IPluginInstallerService> _logger;
	private readonly II18nService _i18n;
	private readonly string _pluginsDir, _stagingDir;

	public PluginInstallerService(ILogger<IPluginInstallerService> logger, II18nService i18n, IPathOptions paths)
	{
		_logger = logger;
		_i18n = i18n;
		_pluginsDir = paths.PluginPath;
		_stagingDir = paths.PluginStagingPath;
	}

	public async AFSTask ExecutePendingOperationsAsync<T>(IProgress<T> progress, CancellationToken cancellationToken = default)
	{
		var progReport = (StartupProgressProxy)(IProgress<StartupProgressProxy>)progress;
		string manifestPath = Path.Combine(_stagingDir, ManifestFileName);
		if (!File.Exists(manifestPath))
		{
			_logger.LogDebug("No pending plugin operations found.");
			return;
		}
		await ManifestLock.WaitAsync(cancellationToken);
		try
		{
			// Load manifest, it is a JArray<InstallerOperation>
			var pendingInstallations = JsonConvert.DeserializeObject<List<InstallerOperation>>(
				await File.ReadAllTextAsync(manifestPath, new UTF8Encoding(false), cancellationToken)) ?? [];
			if (pendingInstallations.Count == 0)
			{
				_logger.LogDebug("No pending plugin operations found in staging manifest.");
				return;
			}

			var execOrder = new[] {OperationType.Uninstall, OperationType.Install}; // first uninstall, then install
			var operations = pendingInstallations
				.OrderBy(o => Array.IndexOf(execOrder, o.Type))
				.ThenBy(o => o.ScheduledAtUtc)
				.ToList();
			foreach (var operation in operations)
			{
				cancellationToken.ThrowIfCancellationRequested();
				ExecuteSinglePendingOperation(progReport, operation);
			}

			File.Delete(manifestPath);
		}
		catch (TaskCanceledException)
		{
			_logger.LogWarning("Plugin installation operations execution was canceled.");
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error executing pending plugin operations.");
			throw;
		}
		finally
		{
			ManifestLock.Release();
		}
	}

	public async IAsyncEnumerable<InstallerOperation> GetPendingInstallationsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		string manifestFullPath = Path.Combine(_stagingDir, ManifestFileName);
		if (!File.Exists(manifestFullPath))
		{
			yield break;
		}

		await ManifestLock.WaitAsync(cancellationToken);
		try
		{
			var operations = JsonConvert.DeserializeObject<List<InstallerOperation>>(
				await File.ReadAllTextAsync(manifestFullPath, new UTF8Encoding(false), cancellationToken)) ?? [];
			foreach (var op in operations.Where(op => op.Type == OperationType.Install))
			{
				yield return op;
			}
		}
		finally
		{
			ManifestLock.Release();
		}
	}

	public async IAsyncEnumerable<InstallerOperation> GetPendingUninstallationsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		string manifestFullPath = Path.Combine(_stagingDir, ManifestFileName);
		if (!File.Exists(manifestFullPath))
		{
			yield break;
		}

		await ManifestLock.WaitAsync(cancellationToken);
		try
		{
			var operations = JsonConvert.DeserializeObject<List<InstallerOperation>>(
				await File.ReadAllTextAsync(manifestFullPath, new UTF8Encoding(false), cancellationToken)) ?? [];
			foreach (var op in operations.Where(op => op.Type == OperationType.Uninstall))
			{
				yield return op;
			}
		}
		finally
		{
			ManifestLock.Release();
		}
	}

	public async AFSTask ScheduleInstallAsync(string srcPackagePath, string dstPackagePath, CancellationToken cancellationToken = default)
	{
		string fileName = Path.GetFileName(srcPackagePath);
		if (string.IsNullOrEmpty(fileName))
		{
			throw new ArgumentException("Source package path is invalid.", nameof(srcPackagePath));
		}
		var pluginManifest = ParseManifest(srcPackagePath);
		var operations = new List<InstallerOperation>();
		if (File.Exists(Path.Combine(_stagingDir, ManifestFileName)))
		{
			var existingOperations = JsonConvert.DeserializeObject<List<InstallerOperation>>(
				await File.ReadAllTextAsync(Path.Combine(_stagingDir, ManifestFileName), new UTF8Encoding(false), cancellationToken)) ?? [];
			if (existingOperations.Count > 0)
			{
				operations.AddRange(existingOperations);
			}
		}

		operations.Add(new InstallerOperation()
		{
			Type = OperationType.Install,
			PluginId = pluginManifest.PluginId,
			SourcePath = srcPackagePath,
			DestinationPath = dstPackagePath,
			ScheduledAtUtc = DateTimeOffset.UtcNow
		});
		await ManifestLock.WaitAsync(cancellationToken);
		await File.WriteAllTextAsync(Path.Combine(_stagingDir, ManifestFileName),
			JsonConvert.SerializeObject(operations, Formatting.Indented), new UTF8Encoding(false), cancellationToken);
		ManifestLock.Release();
		_logger.LogInformation("Scheduled installation of plugin '{PluginId}' from '{SourcePath}' to '{DestinationPath}'.",
			pluginManifest.PluginId, srcPackagePath, dstPackagePath);
	}

	public async AFSTask ScheduleUninstallAsync(string pluginId, string pluginPackagePath, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(pluginId))
		{
			throw new ArgumentException("Plugin ID is invalid.", nameof(pluginId));
		}

		if (!File.Exists(pluginPackagePath) || new FileInfo(pluginPackagePath).Directory?.FullName != Path.GetFullPath(_stagingDir))
		{
			throw new ArgumentException("Plugin package path is invalid or does not exist.", nameof(pluginPackagePath));
		}
		
		var operations = new List<InstallerOperation>();
		string manifestFullPath = Path.Combine(_stagingDir, ManifestFileName);
		
		if (File.Exists(manifestFullPath))
		{
			var existingOperations = JsonConvert.DeserializeObject<List<InstallerOperation>>(
				await File.ReadAllTextAsync(manifestFullPath, new UTF8Encoding(false), cancellationToken)) ?? [];
			if (existingOperations.Count > 0)
			{
				operations.AddRange(existingOperations);
			}
		}
		
		operations.Add(new InstallerOperation()
		{
			Type = OperationType.Uninstall,
			PluginId = pluginId,
			SourcePath = string.Empty,
			DestinationPath = pluginPackagePath,
			ScheduledAtUtc = DateTimeOffset.UtcNow
		});
		
		await ManifestLock.WaitAsync(cancellationToken);
		try
		{
			await File.WriteAllTextAsync(manifestFullPath,
				JsonConvert.SerializeObject(operations, Formatting.Indented), new UTF8Encoding(false), cancellationToken);
			_logger.LogInformation("Scheduled uninstallation of plugin '{PluginId}' at '{PackagePath}'.",
				pluginId, pluginPackagePath);
		}
		finally
		{
			ManifestLock.Release();
		}
	}

	private void ExecuteSinglePendingOperation(StartupProgressProxy progress, InstallerOperation operation)
	{
		switch (operation.Type)
		{
			case OperationType.Install:
			{
				ExecutingOperation?.Invoke(this, operation);
				_logger.LogInformation("Installing plugin from '{SourcePath}' to '{DestinationPath}'", operation.SourcePath, operation.DestinationPath);
				progress.Report(new StartupProgressReport(StringExtension.AdvancedFormat(_i18n["splash.init.plugin.pending.install"].ToString(),
					operation.PluginId), StartupStage.PendingPluginOperations, isAutoLocalized: false));
				if (!File.Exists(operation.SourcePath))
				{
					_logger.LogWarning("Source package '{SourcePath}' does not exist. Skipping installation.", operation.SourcePath);
					return;
				}
				// Ensure destination directory exists
				string? dstDir = Path.GetDirectoryName(operation.DestinationPath);
				if (!string.IsNullOrEmpty(dstDir) && !Directory.Exists(dstDir))
				{
					Directory.CreateDirectory(dstDir);
				}
				// Move file
				File.Move(operation.SourcePath, operation.DestinationPath, overwrite: true);
				_logger.LogInformation("Plugin installed to '{DestinationPath}'", operation.DestinationPath);
				break;
			}
			case OperationType.Uninstall:
			{
				ExecutingOperation?.Invoke(this, operation);
				_logger.LogInformation("Uninstalling plugin at '{PackagePath}'", operation.DestinationPath);
				progress.Report(new StartupProgressReport(StringExtension.AdvancedFormat(_i18n["splash.init.plugin.pending.uninstall"].ToString(),
					operation.PluginId), StartupStage.PendingPluginOperations, isAutoLocalized: false));
				if (File.Exists(operation.DestinationPath))
				{
					File.Delete(operation.DestinationPath);
					if (Directory.Exists(Path.Combine(_pluginsDir, operation.PluginId)))
					{
						Directory.Delete(Path.Combine(_pluginsDir, operation.PluginId), recursive: true); // remove plugin data folder if exists
					}
					_logger.LogInformation("Plugin '{PluginId}' uninstalled.", operation.PluginId);
				}
				else
				{
					_logger.LogWarning("Plugin package '{PackagePath}' does not exist. Skipping uninstallation.", operation.DestinationPath);
				}
				break;
			}
			default:
				_logger.LogWarning("Unknown installer operation type: {OperationType}", operation.Type);
				break;
		}
	}

	private AFSPluginInfo ParseManifest(string pluginPkgPath)
	{
		try
		{
			using var archive = ZipArchive.Open(pluginPkgPath);
			if (archive.TryGetEntry(PluginService.ManifestFileName, out var manifestEntry) && manifestEntry != null)
			{
				using var manifestStream = manifestEntry.OpenEntryStream();
				using var reader = new StreamReader(manifestStream, new UTF8Encoding(false));
				string manifestJson = reader.ReadToEnd();
				var manifest = JsonConvert.DeserializeObject<AFSPluginInfo>(manifestJson)
				               ?? throw new InvalidDataException("Failed to deserialize plugin manifest.");
				manifest.InstallPath = string.Empty; // Will be set by caller
				return manifest;
			}
			else
			{
				throw new FileNotFoundException($"Plugin manifest '{PluginService.ManifestFileName}' not found in package.");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to parse plugin manifest from package '{PluginPkgPath}'.", pluginPkgPath);
			throw new InvalidOperationException("Failed to parse plugin manifest.", ex);
		}
	}
}