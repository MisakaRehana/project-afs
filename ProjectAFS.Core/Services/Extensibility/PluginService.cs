// ReSharper disable NotAccessedField.Local
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using Avalonia;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProjectAFS.Core.Abstracts.Services.Configuration;
using ProjectAFS.Core.Abstracts.Services.Extensibility;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.Models.Extensibility;
using ProjectAFS.Core.Models.Startup;
using ProjectAFS.Core.Services.Startup;
using ProjectAFS.Core.Utility.SharpCompress;
using ProjectAFS.Core.Utility.Threading;
using ProjectAFS.Core.Utility.Collections;
using ProjectAFS.Core.Utility.Strings;
using SharpCompress.Archives.Zip;

namespace ProjectAFS.Core.Services.Extensibility;

/// <summary>
/// Represents a specialized <see cref="AssemblyLoadContext"/> for loading plugin assemblies and their dependencies.
/// </summary>
public sealed class PluginAssemblyLoadContext : AssemblyLoadContext, IDisposable
{
	private readonly AssemblyDependencyResolver _resolver;
	private readonly string _pluginPath;
	private readonly ZipArchive _pluginArchive;
	private bool isDisposed;

	public PluginAssemblyLoadContext(string pluginPath) : base(isCollectible: true)
	{
		isDisposed = false;
		_pluginPath = pluginPath;
		_resolver = new AssemblyDependencyResolver(pluginPath);
		_pluginArchive = ZipArchive.Open(pluginPath);
	}

	protected override Assembly? Load(AssemblyName assemblyName)
	{
		// prefer to load from plugin private dependencies first
		var entryPath = $"lib/{assemblyName.Name}.dll";
		// var entry = _pluginArchive.GetEntry(entryPath);
		if (_pluginArchive.TryGetEntry(entryPath, out var entry) && entry != null)
		{
			using var ms = new MemoryStream();
			using var es = entry.OpenEntryStream();
			es.CopyTo(ms);
			ms.Seek(0, SeekOrigin.Begin);
			return LoadFromStream(ms, assemblySymbols: null); // assemblySymbols is legacy called 'pdbStream', we don't have symbol files for plugins.
		}
		
		string? path = _resolver.ResolveAssemblyToPath(assemblyName);
		if (path != null)
		{
			return LoadFromAssemblyPath(path); // allow plugins to use native dependencies and/or runtimeconfig.json if needed
		}

		return null; // try to resolve via default context (shared libraries)
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	
	private void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			if (disposing)
			{
				_pluginArchive.Dispose();
			}
			isDisposed = true;
		}
	}
	
	~PluginAssemblyLoadContext()
	{
		Dispose(false);
	}
}

/// <summary>
/// Represents a context for a loaded plugin.
/// </summary>
public sealed class PluginContext : IDisposable
{
	public AFSPluginInfo Info { get; }
	public string EntryAssembly { get; }
	public PluginAssemblyLoadContext? LoadContext { get; private set; }

	public IPlugin? Instance { get; set; }
	public bool IsLoaded => LoadContext != null;

	private bool isDisposed;

	public PluginContext(AFSPluginInfo info, string entryAssembly)
	{
		isDisposed = false;
		Info = info;
		EntryAssembly = entryAssembly;
	}

	public void SetStatus(PluginStatus status)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		Info.Status = status;
	}
	
	public void SetLoadContext(PluginAssemblyLoadContext loadContext)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		LoadContext = loadContext;
	}
	
	public void RegisterInstance(IPlugin instance)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		Instance = instance;
	}
	
	public void Unload()
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		LoadContext?.Unload();
		LoadContext = null;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	
	private void Dispose(bool disposing)
	{
		if (isDisposed) return;
		if (disposing)
		{
			Instance?.Dispose();
		}
		Instance = null;
		isDisposed = true;
	}

	~PluginContext()
	{
		Dispose(false);
	}
}

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

/// <summary>
/// Represents the plugin service responsible for managing plugins.
/// </summary>
public sealed class PluginService : IHostedService, IPluginService
{
	public const string ManifestFileName = "plugin.json";
	private const string PluginExt = ".afp";

	public event EventHandler<PluginEventArgs>? PluginLoading;
	public event EventHandler<PluginEventArgs>? PluginLoaded;
	public event EventHandler<PluginEventArgs>? PluginUnloaded;
	public event EventHandler<PluginEventArgs>? PluginInstallationScheduled;
	public event EventHandler<PluginEventArgs>? PluginUninstallationScheduled;
	
	private readonly StartupProgressProxy _progress;
	private readonly IAFSConfiguration _config;
	private readonly II18nService _i18n;
	private readonly ILogger<PluginService> _logger;
	private readonly IPathOptions _paths;
	private readonly IPluginInstallerService _installer;
	private readonly ConcurrentDictionary<string, PluginContext> _plugins = new();
	private readonly ConcurrentDictionary<string, SemaphoreSlim> _pluginLocks = new();
	private readonly ConcurrentHashSet<string> _enabledPlugins = [];
	
	public PluginService(StartupProgressProxy progress, IAFSConfiguration config, II18nService i18n, ILogger<PluginService> logger, IPathOptions paths, IPluginInstallerService installer)
	{
		_progress = progress;
		_config = config;
		_i18n = i18n;
		_logger = logger;
		_paths = paths;
		_installer = installer;
	}
	
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		LoadEnabledPluginsConfiguration();
		await ExecutePendingOperationsAsync(cancellationToken: cancellationToken);
		await LoadPluginsAsync(cancellationToken: cancellationToken);
	}
	
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		SaveEnabledPluginsConfiguration();
		await AFSTask.WhenAll(_plugins.Values
			.Where(pc => pc.IsLoaded)
			.Select(pc => UnloadSinglePluginAsync(pc.Info.PluginId, cancellationToken)));
	}

	public IEnumerable<AFSPluginInfo> DiscoverPlugins()
	{
		_plugins.Clear();
		if (!Directory.Exists(_paths.PluginPath))
		{
			_logger.LogDebug("Creating directory for plugins at {PluginPath}, because cannot find plugins folder!", _paths.PluginPath);
			Directory.CreateDirectory(_paths.PluginPath);
			return [];
		}
		var pluginPackages = Directory.GetFiles(_paths.PluginPath, $"*{PluginExt}", SearchOption.TopDirectoryOnly);

		foreach (string packagePath in pluginPackages)
		{
			try
			{
				using var archive = ZipArchive.Open(packagePath);
				if (!archive.TryGetEntry(ManifestFileName, out var manifestEntry) || manifestEntry == null)
				{
					_logger.LogWarning("Plugin package {PackagePath} is missing the manifest file {ManifestFileName}. Skipping.", packagePath, ManifestFileName);
					continue;
				}

				using var reader = new StreamReader(manifestEntry.OpenEntryStream(), new UTF8Encoding(false));
				var manifest = JsonConvert.DeserializeObject<AFSPluginInfo>(reader.ReadToEnd()) ??
				               throw new InvalidDataException($"Failed to deserialize plugin manifest from {ManifestFileName} in package {packagePath}.");
				manifest.InstallPath = packagePath;

				string entryAssembly = manifest.EntryPointLibrary;
				if (string.IsNullOrEmpty(entryAssembly))
				{
					_logger.LogWarning("Plugin {PluginId} does not specify an entry point library. Skipping.", manifest.PluginId);
					continue;
				}

				manifest.Status = _enabledPlugins.Contains(manifest.PluginId) ? PluginStatus.Enabled : PluginStatus.Disabled;

				var context = new PluginContext(manifest, entryAssembly);
				_plugins.TryAdd(manifest.PluginId, context);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to load plugin package {PackagePath}.", packagePath);
			}
		}
		
		return _plugins.Values.Select(pc => pc.Info);
	}
	
	public async AFSTask ExecutePendingOperationsAsync(CancellationToken cancellationToken = default)
	{
		await _installer.ExecutePendingOperationsAsync(_progress, cancellationToken);
	}

	public async AFSTask LoadPluginsAsync(bool skipDiscovering = false, CancellationToken cancellationToken = default)
	{
		if (!skipDiscovering)
		{
			DiscoverPlugins();
		}

		var pluginsToLoad = GetPluginsInDependencyOrder();
		foreach (var plugin in pluginsToLoad)
		{
			if (cancellationToken.IsCancellationRequested) break;
			if (plugin.Status != PluginStatus.Enabled) continue;
			
			await LoadSinglePluginAsync(plugin.PluginId, cancellationToken);
		}
	}

	private async AFSTask<PluginContext?> LoadSinglePluginAsync(string pluginId, CancellationToken cancellationToken = default)
	{
		if (!_plugins.TryGetValue(pluginId, out var ctx) || ctx.IsLoaded)
		{
			return null;
		}
		
		var locker = _pluginLocks.GetOrAdd(pluginId, _ => new SemaphoreSlim(1, 1));
		await locker.WaitAsync(cancellationToken);

		try
		{
			if (ctx.IsLoaded) return ctx; // double-check after acquiring the lock
			if (!ValidateDependencies(pluginId, out var missingDeps))
			{
				ctx.SetStatus(PluginStatus.Faulted);
				string depList = string.Join("; ", missingDeps);
				_logger.LogError("Cannot load plugin {PluginId} due to missing dependencies: {DependencyList}", pluginId, depList);
				return null;
			}

			ctx.SetStatus(PluginStatus.Loading);
			PluginLoading?.Invoke(this, new PluginEventArgs(pluginId, PluginStatus.Loading)
			{
				PluginName = ctx.Info.Name
			});
			_progress.Report(new StartupProgressReport(StringExtension.AdvancedFormat(
				_i18n["splash.init.plugin"].ToString(), ctx.Info.Name), StartupStage.PluginLoading, isAutoLocalized: false));

			var alc = new PluginAssemblyLoadContext(ctx.Info.InstallPath);
			var assembly = alc.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(ctx.EntryAssembly)));

			var pluginBaseType = typeof(IPlugin);
			var pluginTypes = assembly.GetTypes()
				.Where(t => pluginBaseType.IsAssignableFrom(t) && t is {IsAbstract: false, IsClass: true})
				.ToList();
			if (pluginTypes.Count == 0)
			{
				throw new InvalidOperationException($"No valid plugin class found in assembly '{ctx.EntryAssembly}' for plugin '{pluginId}'.");
			}

			if (pluginTypes.Count > 1)
			{
				throw new InvalidOperationException($"Duplicate plugin classes found in assembly '{ctx.EntryAssembly}' for plugin '{pluginId}'. Only one plugin class is allowed to represent the plugin.");
			}

			var app = (Application.Current as AFSApp)!;
			var pluginType = pluginTypes.Single();
			var pluginInstance = app.CreateInstanceWithInjection(pluginType) as IPlugin;
			if (pluginInstance == null)
			{
				throw new InvalidOperationException($"Failed to create an instance of plugin class '{pluginType.FullName}' in assembly '{ctx.EntryAssembly}' for plugin '{pluginId}'.");
			}

			ctx.SetLoadContext(alc);
			ctx.RegisterInstance(pluginInstance);
			ctx.SetStatus(PluginStatus.Enabled);
			PluginLoaded?.Invoke(this, new PluginEventArgs(pluginId, PluginStatus.Enabled)
			{
				PluginName = ctx.Info.Name
			});
			return ctx;
		}
		catch (Exception ex)
		{
			ctx.SetStatus(PluginStatus.Faulted);
			_logger.LogError(ex, "Failed to load plugin {PluginId}.", pluginId);
			return null;
		}
		finally
		{
			locker.Release();
		}
	}

	private async AFSTask UnloadSinglePluginAsync(string pluginId, CancellationToken cancellationToken = default)
	{
		if (!_plugins.TryGetValue(pluginId, out var ctx) || !ctx.IsLoaded)
		{
			return;
		}

		var dependents = _plugins.Values
			.Where(pc => pc.IsLoaded && pc.Info.Dependencies.Any(d => d.PluginId == pluginId))
			.ToList();
		if (dependents.Count != 0)
		{
			string depList = string.Join(", ", dependents.Select(dc => $"{dc.Info.Name} ({dc.Info.PluginId})"));
			throw new InvalidOperationException($"Cannot unload plugin '{pluginId}' because it is required by other loaded plugins: {depList}");
		}
		
		var locker = _pluginLocks.GetOrAdd(pluginId, _ => new SemaphoreSlim(1, 1));
		await locker.WaitAsync(cancellationToken);
		try
		{
			if (!ctx.IsLoaded) return; // double-check after acquiring the lock

			ctx.SetStatus(PluginStatus.Unloading);

			ctx.Instance?.Dispose();
			ctx.Instance = null;

			ctx.Unload();

			for (int i = 0; i < 3; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
			}
			
			ctx.SetStatus(PluginStatus.Enabled); // back to enabled state, can be loaded again later
			PluginUnloaded?.Invoke(this, new PluginEventArgs(pluginId, PluginStatus.Enabled)
			{
				PluginName = ctx.Info.Name
			});
		}
		finally
		{
			locker.Release();
		}
	}
	
	private void LoadEnabledPluginsConfiguration()
	{
		try
		{
			var section = _config.GetSection("Plugins");
			if (section.Settings.TryGetValue("EnabledPlugins", out string? json) && !string.IsNullOrEmpty(json))
			{
				var enabledPlugins = JsonConvert.DeserializeObject<List<string>>(json);
				if (enabledPlugins != null)
				{
					_enabledPlugins.Clear();
					foreach (string pluginId in enabledPlugins)
					{
						_enabledPlugins.TryAdd(pluginId);
					}
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load enabled plugins configuration.");
		}
	}
	
	private void SaveEnabledPluginsConfiguration()
	{
		try
		{
			string json = JsonConvert.SerializeObject(_enabledPlugins, Formatting.Indented);
			var section = _config.GetSection("Plugins");
			section.Settings["EnabledPlugins"] = json;
			_config.Save(section);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to save enabled plugins configuration.");
		}
	}

	public async AFSTask ScheduleNewPluginInstallAsync(string pluginPackagePath, CancellationToken cancellationToken = default)
	{
		if (!File.Exists(pluginPackagePath) || Path.GetExtension(pluginPackagePath) != PluginExt)
		{
			throw new FileNotFoundException("Plugin package file not found or invalid.", pluginPackagePath);
		}
		string tempFileName = Guid.NewGuid().ToString("N") + PluginExt;
		string stagingPath = Path.Combine(_paths.PluginStagingPath, tempFileName);
		
		File.Copy(pluginPackagePath, stagingPath, overwrite: true);
		
		await _installer.ScheduleInstallAsync(stagingPath, Path.Combine(_paths.PluginPath, tempFileName), cancellationToken); // will popup a notice that 'Changes had been scheduled. Close all project-afs windows to start executing changes.'
		
		PluginInstallationScheduled?.Invoke(this, new PluginEventArgs(Path.GetFileNameWithoutExtension(tempFileName), PluginStatus.Disabled));
	}

	public async AFSTask SchedulePluginUninstallAsync(string pluginId, CancellationToken cancellationToken = default)
	{
		if (!_plugins.TryGetValue(pluginId, out var ctx))
		{
			throw new KeyNotFoundException($"Plugin '{pluginId}' not found.");
		}

		if (ctx.IsLoaded)
		{
			await UnloadSinglePluginAsync(pluginId, cancellationToken);
			string pluginPackagePath = ctx.Info.InstallPath;
			await _installer.ScheduleUninstallAsync(pluginId, pluginPackagePath, cancellationToken); // will popup a notice that 'Changes had been scheduled. Close all project-afs windows to start executing changes.'
			PluginUninstallationScheduled?.Invoke(this, new PluginEventArgs(pluginId, PluginStatus.Disabled));
		}
	}

	public void EnablePlugin(string pluginId)
	{
		if (!_plugins.ContainsKey(pluginId))
		{
			throw new KeyNotFoundException($"Plugin '{pluginId}' not found.");
		}

		if (_enabledPlugins.TryAdd(pluginId))
		{
			SaveEnabledPluginsConfiguration();
			if (_plugins.TryGetValue(pluginId, out var ctx) && ctx.Instance != null)
			{
				try
				{
					ctx.Instance.OnEnable();
					ctx.SetStatus(PluginStatus.Enabled);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error occurred while enabling plugin {PluginId}.", pluginId);
					ctx.SetStatus(PluginStatus.Faulted);
				}
			}
		}
	}
	
	public void DisablePlugin(string pluginId)
	{
		if (_enabledPlugins.Remove(pluginId))
		{
			SaveEnabledPluginsConfiguration();
			if (_plugins.TryGetValue(pluginId, out var ctx) && ctx.Instance != null)
			{
				try
				{
					ctx.Instance.OnDisable();
					ctx.SetStatus(PluginStatus.Disabled);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error occurred while disabling plugin {PluginId}.", pluginId);
					ctx.SetStatus(PluginStatus.Faulted);
				}
			}
		}
	}

	private bool ValidateDependencies(string pluginId, out IEnumerable<string> missingDependencies)
	{
		var missing = new List<string>();
		missingDependencies = missing;

		if (!_plugins.TryGetValue(pluginId, out var ctx))
		{
			missing.Add($"Plugin '{pluginId}' itself is not found.");
			return false;
		}

		foreach (var dep in ctx.Info.Dependencies)
		{
			if (!_plugins.TryGetValue(dep.PluginId, out var depCtx))
			{
				if (!dep.IsOptional)
				{
					missing.Add($"Required dependency '{dep.PluginId}' is not installed.");
				}
				continue;
			}

			if (!dep.IsVersionSatisfied(depCtx.Info.Version) && !dep.IsOptional)
			{
				missing.Add($"Dependency '{dep.PluginId}' required by plugin '{pluginId}' requires version '{dep.VersionConstraint}' but found '{depCtx.Info.Version}'.");
			}
		}
		return missing.Count == 0;
	}

	private List<AFSPluginInfo> GetPluginsInDependencyOrder()
	{
		var sorted = new List<AFSPluginInfo>();
		var visited = new HashSet<string>();
		var visiting = new HashSet<string>(); // to detect cycle-dependency
		var pluginsToProcess = _plugins.Values.Where(pc => pc.Info.Status == PluginStatus.Enabled);

		foreach (var context in pluginsToProcess)
		{
			if (!visited.Contains(context.Info.PluginId))
			{
				TopologicalSortIterative(context, visited, visiting, sorted);
			}
		}
		
		return sorted;
	}

	private void TopologicalSortIterative(PluginContext startContext, HashSet<string> visited, HashSet<string> visiting, List<AFSPluginInfo> sorted)
	{
		// based on Depth-First Search (DFS) Topological Sort algorithm
		var stack = new Stack<(PluginContext ctx, bool processed)>();
		stack.Push((startContext, false));

		while (stack.Count > 0)
		{
			(var ctx, bool processed) = stack.Pop();
			string id = ctx.Info.PluginId;

			if (processed)
			{
				visiting.Remove(id);
				visited.Add(id);
				sorted.Add(ctx.Info);
				continue;
			}
			
			if (visited.Contains(id)) continue;

			if (!visiting.Add(id))
			{
				throw new InvalidOperationException($"Cyclic dependency detected at plugin: '{id}'");
			}
			
			stack.Push((ctx, true)); // mark as "process me after process all my dependencies"

			var deps = ctx.Info.Dependencies;
			if (deps is {Count: > 0})
			{
				// reversing push into stack to ensure same as recursive order
				for (int i = deps.Count - 1; i >= 0; i--)
				{
					var dep = deps[i];
					if (dep.IsOptional) continue;

					if (_plugins.TryGetValue(dep.PluginId, out var depContext))
					{
						if (!visited.Contains(depContext.Info.PluginId))
						{
							stack.Push((depContext, false));
						}
					}
					else
					{
						throw new InvalidOperationException($"Missing dependency: '{dep.PluginId}' required by plugin '{id}'");
					}
				}
			}
		}
	}
}