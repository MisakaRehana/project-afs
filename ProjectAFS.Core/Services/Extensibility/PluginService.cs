// ReSharper disable NotAccessedField.Local
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
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
	
	private readonly AFSApp _app;
	private readonly StartupProgressProxy _progress;
	private readonly IAFSConfiguration _config;
	private readonly II18nService _i18n;
	private readonly ILogger<PluginService> _logger;
	private readonly IPathOptions _paths;
	private readonly IPluginInstallerService _installer;
	private readonly ConcurrentBag<Type> _builtInPlugins = [];
	private readonly ConcurrentDictionary<string, PluginContext> _plugins = new();
	private readonly ConcurrentDictionary<string, SemaphoreSlim> _pluginLocks = new();
	private readonly ConcurrentHashSet<string> _enabledPlugins = [];
	
	public PluginService(AFSApp app, StartupProgressProxy progress, IAFSConfiguration config, II18nService i18n, ILogger<PluginService> logger, IPathOptions paths, IPluginInstallerService installer)
	{
		_app = app;
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
	
	private void DiscoverBuiltInPlugins()
	{
		_builtInPlugins.Clear();
		var plugins = _app.BuiltInPlugins;
		foreach (var pluginType in plugins)
		{
			RegisterBuiltInPlugin(pluginType);
			_builtInPlugins.Add(pluginType);
		}
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
	
	public void RegisterBuiltInPlugin<TPlugin>() where TPlugin : class, IPlugin, new()
	{
		_builtInPlugins.Add(typeof(TPlugin));
	}
	
	public void RegisterBuiltInPlugin(Type pluginType)
	{
		if (!typeof(IPlugin).IsAssignableFrom(pluginType))
		{
			throw new ArgumentException("The specified type does not implement IPlugin interface.", nameof(pluginType));
		}
		_builtInPlugins.Add(pluginType);
	}
	
	public async AFSTask ExecutePendingOperationsAsync(CancellationToken cancellationToken = default)
	{
		await _installer.ExecutePendingOperationsAsync(_progress, cancellationToken);
	}

	public async AFSTask LoadPluginsAsync(bool skipDiscovering = false, CancellationToken cancellationToken = default)
	{
		if (!skipDiscovering)
		{
			DiscoverBuiltInPlugins();
			DiscoverPlugins();
		}
		
		LoadBuiltInPlugins();

		var pluginsToLoad = GetPluginsInDependencyOrder();
		foreach (var plugin in pluginsToLoad.Where(p => !p.IsBuiltIn))
		{
			if (cancellationToken.IsCancellationRequested) break;
			if (plugin.Status != PluginStatus.Enabled) continue;
			
			await LoadSinglePluginAsync(plugin.PluginId, cancellationToken);
		}
	}
	
	private void LoadBuiltInPlugins()
	{
		foreach (var pluginType in _builtInPlugins)
		{
			var info = new AFSPluginInfo()
			{
				PluginId = pluginType.FullName!,
				Name = $"{pluginType.Name} (Built-in)",
				Version = new Version(1, 0, 0, 0),
				Status = PluginStatus.Enabled,
				IsBuiltIn = true
			};
			
			var ctx = new PluginContext(info, string.Empty);
			
			var instance = _app.CreateInstanceWithInjection(pluginType) as IPlugin;
			
			ctx.RegisterInstance(instance!);
			ctx.SetStatus(PluginStatus.Enabled);
			
			_plugins.TryAdd(info.PluginId, ctx);
			
			instance!.OnEnable();
			
			_logger.LogInformation("Built-in plugin {PluginId} loaded.", info.PluginId);
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

			var pluginType = pluginTypes.Single();
			var pluginInstance = _app.CreateInstanceWithInjection(pluginType) as IPlugin;
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