using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProjectAFS.Core.Abstractions.Configuration;
using ProjectAFS.Core.Abstractions.Plugins;

namespace ProjectAFS.Core.Internal.Plugins;

public class PluginInstaller : IPluginInstaller
{
	private readonly static SemaphoreSlim ManifestLock = new(1, 1);
	private readonly ILogger<PluginInstaller> _logger;
	private readonly string _pluginsDir;
	private readonly string _stagingDir;
	private readonly string _installManifestPath;
	private readonly string _pluginManifestName;

	public PluginInstaller(ILogger<PluginInstaller> logger, IPathOptions pathOptions)
	{
		_logger = logger;
		_pluginsDir = pathOptions.PluginPath;
		_stagingDir = pathOptions.PluginStagingPath;
		_installManifestPath = pathOptions.PluginPendingStateFileName;
		_pluginManifestName = pathOptions.PluginManifestFileName;
		Task.Run(() => ExecutePendingOperationsAsync()); // no await
	}

	public async Task ScheduleInstallAsync(string srcPackagePath, string dstPackagePath, CancellationToken cancellationToken = default)
	{
		var fileName = Path.GetFileName(srcPackagePath);
		if (string.IsNullOrEmpty(fileName))
		{
			throw new ArgumentException("Source package path is invalid.", nameof(srcPackagePath));
		}
		var pluginManifest = ParseManifest(srcPackagePath);
		var operations = new List<InstallerOperation>();
		if (File.Exists(Path.Combine(_stagingDir, _installManifestPath)))
		{
			var existingOperations = JsonConvert.DeserializeObject<List<InstallerOperation>>(
				                         await File.ReadAllTextAsync(Path.Combine(_stagingDir, _installManifestPath), Encoding.UTF8, cancellationToken))
			                         ?? [];
			if (existingOperations.Count > 0)
			{
				operations.AddRange(existingOperations);
			}
		}
		operations.Add(new InstallerOperation()
		{
			Type = OperationType.Install,
			PluginId = pluginManifest.Id,
			SourcePath = srcPackagePath,
			DestinationPath = dstPackagePath,
			ScheduledAtUtc = DateTimeOffset.UtcNow
		});
		await ManifestLock.WaitAsync(cancellationToken);
		await File.WriteAllTextAsync(Path.Combine(_stagingDir, _installManifestPath), JsonConvert.SerializeObject(operations, Formatting.Indented), Encoding.UTF8, cancellationToken);
		ManifestLock.Release();
		_logger.LogInformation("Scheduled installation of plugin '{PluginId}' from '{SourcePath}' to '{DestinationPath}'", pluginManifest.Id, srcPackagePath, dstPackagePath);
	}

	public async Task ScheduleUninstallAsync(string pluginId, string pluginPackagePath, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(pluginId))
		{
			throw new ArgumentException("Plugin ID is invalid.", nameof(pluginId));
		}
		
		if (!File.Exists(pluginPackagePath) || new FileInfo(pluginPackagePath).Directory?.FullName != Path.GetFullPath(_stagingDir))
		{
			throw new ArgumentException("Plugin package path is invalid or does not exist.", nameof(pluginPackagePath));
		}
		
		var operations = new List<InstallerOperation>();
		string manifestFullPath = Path.Combine(_stagingDir, _installManifestPath);
		
		if (File.Exists(manifestFullPath))
		{
			var existingOperations = JsonConvert.DeserializeObject<List<InstallerOperation>>(
				                         await File.ReadAllTextAsync(manifestFullPath, Encoding.UTF8, cancellationToken))
			                         ?? [];
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
			ScheduledAtUtc = DateTimeOffset.Now
		});
		
		await ManifestLock.WaitAsync(cancellationToken);
		try
		{
			await File.WriteAllTextAsync(manifestFullPath, JsonConvert.SerializeObject(operations, Formatting.Indented), Encoding.UTF8, cancellationToken);

			_logger.LogInformation("Scheduled uninstallation of plugin '{PluginId}' at '{PackagePath}'", pluginId, pluginPackagePath);
		}
		finally
		{
			ManifestLock.Release();
		}
	}

	public async Task ExecutePendingOperationsAsync(CancellationToken cancellationToken = default)
	{
		string manifestFullPath = Path.Combine(_stagingDir, _installManifestPath);
		if (!File.Exists(manifestFullPath))
		{
			_logger.LogDebug("No pending plugin operations found.");
			return;
		}
		await ManifestLock.WaitAsync(cancellationToken);
		try
		{
			// Load manifest, it is a JArray<InstallerOperation>
			var pendingInstallations = JsonConvert.DeserializeObject<List<InstallerOperation>>(await File.ReadAllTextAsync(manifestFullPath, Encoding.UTF8, cancellationToken))
			                           ?? [];
			if (pendingInstallations.Count == 0)
			{
				_logger.LogDebug("No pending plugin operations found in manifest.");
				return;
			}
			foreach (var operation in pendingInstallations.OrderBy(i => i.ScheduledAtUtc))
			{
				ExecuteSinglePendingOperation(operation);
			}
			File.Delete(manifestFullPath);
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

	private void ExecuteSinglePendingOperation(InstallerOperation operation)
	{
		switch (operation.Type)
		{
			case OperationType.Install:
			{
				_logger.LogInformation("Installing plugin from '{SourcePath}' to '{DestinationPath}'", operation.SourcePath, operation.DestinationPath);
				if (!File.Exists(operation.SourcePath))
				{
					_logger.LogWarning("Source package '{SourcePath}' does not exist. Skipping installation.", operation.SourcePath);
					return;
				}
				// Ensure destination directory exists
				var dstDir = Path.GetDirectoryName(operation.DestinationPath);
				if (!string.IsNullOrEmpty(dstDir) && !Directory.Exists(dstDir))
				{
					Directory.CreateDirectory(dstDir);
				}
				// Move file
				File.Move(operation.SourcePath, operation.DestinationPath, true);
				_logger.LogInformation("Plugin installed to '{DestinationPath}'", operation.DestinationPath);
				break;
			}
			case OperationType.Uninstall:
			{
				_logger.LogInformation("Uninstalling plugin at '{PackagePath}'", operation.DestinationPath);
				if (File.Exists(operation.DestinationPath))
				{
					File.Delete(operation.DestinationPath);
					if (Directory.Exists(Path.Combine(_pluginsDir, operation.PluginId)))
					{
						Directory.Delete(Path.Combine(_pluginsDir, operation.PluginId), true); // remove plugin data folder if exists
					}
					_logger.LogInformation("Plugin '{PackagePath} uninstalled.", operation.DestinationPath);
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

	private PluginInfo ParseManifest(string pluginPkgPath)
	{
		try
		{
			using var archive = new ZipFile(File.OpenRead(pluginPkgPath));
			var manifestEntry = archive.GetEntry(_pluginManifestName) ?? throw new FileNotFoundException($"Plugin manifest '{_pluginManifestName}' not found in package.");
			using var manifestStream = archive.GetInputStream(manifestEntry);
			using var reader = new StreamReader(manifestStream, Encoding.UTF8);
			string manifestJson = reader.ReadToEnd();
			var manifest = JsonConvert.DeserializeObject<PluginInfo>(manifestJson)
			               ?? throw new InvalidDataException("Failed to deserialize plugin manifest.");
			manifest.InstallPath = string.Empty; // Will be set by caller
			return manifest;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to parse plugin manifest from package '{PackagePath}'", pluginPkgPath);
			throw new InvalidOperationException("Failed to parse plugin manifest.", ex);
		}
	}
}