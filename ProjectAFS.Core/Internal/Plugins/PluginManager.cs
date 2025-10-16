using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Avalonia;
using ProjectAFS.Core.Abstractions.Plugins;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProjectAFS.Core.Abstractions;
using ProjectAFS.Core.Abstractions.Configuration;
using ProjectAFS.Core.Abstractions.Localization;
using ProjectAFS.Core.Abstractions.Logging;
using ProjectAFS.Core.Utility.Collections;
using ProjectAFS.Extensibility.Abstractions.Plugins;

namespace ProjectAFS.Core.Internal.Plugins;

/// <summary>
/// Represents the manager responsible for handling project-afs IDE plugins.
/// </summary>
public class PluginManager : IPluginManager, ILoggerClient<PluginManager>, IDisposable, IAsyncDisposable
{
	private const string ManifestFileName = "plugin.json";
	private const string PluginExt = ".afp";
	
	private readonly string _pluginsDir;
	private readonly string _stagingDir;
	private readonly string _enabledConfigFile;
	
	public ILogger<PluginManager> Logger { get; }
	private readonly IPathOptions _pathOptions;
	private readonly II18nExtensibleManager _i18nExtManager;
	private readonly ConcurrentDictionary<string, PluginContext> _plugins = new();
	private readonly ConcurrentDictionary<string, SemaphoreSlim> _pluginLocks = new();
	private ConcurrentHashSet<string> _enabledPluginIds = [];
	private readonly ConcurrentDictionary<string, ConcurrentBag<II18nExtensibleLocalizationLocator>> _pluginI18nLocators = new();
	private bool _isDisposed;

	public event EventHandler<PluginEventArgs>? PluginLoaded;
	public event EventHandler<PluginEventArgs>? PluginUnloaded;
	public event EventHandler<PluginEventArgs>? PluginInstallationScheduled;

	public PluginManager(ILogger<PluginManager> logger, IPathOptions pathOptions, II18nExtensibleManager i18nExtManager)
	{
		Logger = logger;
		_pathOptions = pathOptions;
		_i18nExtManager = i18nExtManager;
		_pluginsDir = pathOptions.PluginPath;
		_stagingDir = pathOptions.PluginStagingPath;
		_enabledConfigFile = Path.Combine(_pluginsDir, pathOptions.PluginStateFileName);
		Directory.CreateDirectory(_pluginsDir);
		Directory.CreateDirectory(_stagingDir);
		LoadEnabledPluginsConfiguration();
	}
	
	public IEnumerable<PluginInfo> DiscoverPlugins()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		_plugins.Clear();
		_pluginI18nLocators.Clear();
		var pluginPackages = Directory.GetFiles(_pluginsDir, $"*{PluginExt}", SearchOption.TopDirectoryOnly);

		foreach (string packagePath in pluginPackages)
		{
			try
			{
				using var zipFile = new ZipFile(packagePath);
				var manifestEntry = zipFile.GetEntry(ManifestFileName);
				if (manifestEntry == null)
				{
					Logger.LogWarning("Plugin package {PackagePath} is missing the manifest file {ManifestFileName}. Skipping.", packagePath, ManifestFileName);
					continue;
				}

				using var reader = new StreamReader(zipFile.GetInputStream(manifestEntry), Encoding.UTF8);
				var manifest = JsonConvert.DeserializeObject<PluginInfo>(reader.ReadToEnd()) ??
				               throw new InvalidDataException($"Failed to deserialize plugin manifest from {ManifestFileName} in package {packagePath}.");
				manifest.InstallPath = packagePath;

				string entryAssembly = manifest.EntryPointLibrary;
				if (string.IsNullOrWhiteSpace(entryAssembly))
				{
					Logger.LogWarning("Plugin {PluginId} does not specify an entry point library. Skipping.", manifest.Id);
					continue;
				}

				manifest.Status = _enabledPluginIds.Contains(manifest.Id) ? PluginStatus.Enabled : PluginStatus.Disabled;

				var context = new PluginContext(manifest, entryAssembly);
				_plugins.TryAdd(manifest.Id, context);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Failed to load plugin package {PackagePath}, skipping.", packagePath);
			}
		}
		return _plugins.Values.Select(p => p.Info);
	}
	public async Task LoadPluginsAsync(CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		DiscoverPlugins();
		var pluginsToLoad = GetPluginsInDependencyOrder();

		foreach (var plugin in pluginsToLoad)
		{
			if (cancellationToken.IsCancellationRequested) break;
			if (plugin.Status != PluginStatus.Enabled) continue;
			
			await LoadSinglePluginAsync(plugin.Id, cancellationToken);
		}
	}
	public async Task UnloadPluginAsync(string pluginId, CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		if (!_plugins.TryGetValue(pluginId, out var ctx) || !ctx.IsLoaded)
		{
			return;
		}
		
		var dependents = _plugins.Values
			.Where(p => p.IsLoaded && p.Info.Dependencies.Any(d => d.PluginId == pluginId))
			.ToList();
		if (dependents.Count != 0)
		{
			string depList = string.Join(", ", dependents.Select(d => $"{d.Info.Name} ({d.Info.Id})"));
			throw new InvalidOperationException($"Cannot unload plugin '{pluginId}'. It is required by: {depList}. Please unload them first.");
		}
		
		var locker = _pluginLocks.GetOrAdd(pluginId, _ => new SemaphoreSlim(1, 1));
		await locker.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			if (!ctx.IsLoaded) return;

			ctx.SetStatus(PluginStatus.Unloading);
			
			if (_pluginI18nLocators.TryRemove(pluginId, out var locators))
			{
				foreach (var locator in locators)
				{
					_i18nExtManager.UnregisterLocator(locator);
				}
			}
			
			ctx.Instance?.Dispose();
			ctx.Instance = null;
			
			ctx.Unload();

			ctx.SetStatus(PluginStatus.Enabled); // back to enabled state, can be loaded again later
			PluginUnloaded?.Invoke(this, new PluginEventArgs(pluginId, PluginStatus.Enabled));
		}
		finally
		{
			locker.Release();
		}
	}
	public IEnumerable<PluginInfo> GetLoadedPlugins()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		return _plugins.Values.Where(p => p.IsLoaded).Select(p => p.Info);
	}
	public IEnumerable<PluginContext> GetLoadedPluginsWithContext()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		return _plugins.Values.Where(p => p.IsLoaded);
	}
	public PluginStatus GetPluginStatus(string pluginId)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		return _plugins.TryGetValue(pluginId, out var ctx) ? ctx.Info.Status : PluginStatus.Disabled;
	}
	public AssemblyLoadContext GetPluginLoadContext(string pluginId)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		if (_plugins.TryGetValue(pluginId, out var ctx) && ctx.IsLoaded)
		{
			return ctx.LoadContext!;
		}
		throw new KeyNotFoundException($"Plugin '{pluginId}' is not loaded or does not exist.");
	}
	public async Task ScheduleNewPluginInstallAsync(string pluginPackagePath, CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		if (!File.Exists(pluginPackagePath) || Path.GetExtension(pluginPackagePath) != PluginExt)
		{
			throw new ArgumentException("Invalid plugin package path.", nameof(pluginPackagePath));
		}
		// string tempFileName = Path.GetFileName(pluginPackagePath);
		string tempFileName = Guid.NewGuid().ToString("N") + PluginExt;
		string stagingPath = Path.Combine(_stagingDir, tempFileName);
		
		File.Copy(pluginPackagePath, stagingPath, overwrite: true);
		
		var host = (Application.Current as IHostingAvaloniaDesktopApp)!.Host;
		var pluginInstaller = host.Services.GetRequiredService<IPluginInstaller>();
		await pluginInstaller.ScheduleInstallAsync(stagingPath, Path.Combine(_pluginsDir, tempFileName), cancellationToken); // will popup a notice that 'Changes had been scheduled. Close all project-afs windows to start executing changes.'
		
		PluginInstallationScheduled?.Invoke(this, new PluginEventArgs(tempFileName, PluginStatus.Disabled));
	}
	public async Task UninstallPluginAsync(string pluginId, CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		if (!_plugins.TryGetValue(pluginId, out var ctx))
		{
			throw new KeyNotFoundException($"Plugin '{pluginId}' not found.");
		}

		if (ctx.IsLoaded)
		{
			await UnloadPluginAsync(pluginId, cancellationToken);
		}
		
		await DisablePluginAsync(pluginId, cancellationToken);
		
		await File.Create(Path.Combine(_stagingDir, $"{pluginId}.remove")).DisposeAsync();
	}
	public async Task EnablePluginAsync(string pluginId, CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		if (!_plugins.ContainsKey(pluginId))
		{
			throw new KeyNotFoundException($"Plugin '{pluginId}' not found.");
		}

		if (_enabledPluginIds.TryAdd(pluginId))
		{
			await SaveEnabledPluginsConfigurationAsync(cancellationToken);
			if (_plugins.TryGetValue(pluginId, out var ctx) && ctx.Instance != null)
			{
				try
				{
					ctx.Instance.OnEnable();
					ctx.SetStatus(PluginStatus.Enabled);
				}
				catch (Exception ex)
				{
					Logger.LogError(ex, "Error occurred while enabling plugin '{PluginId}'.\n{ExceptionString}", pluginId, ex.ToString());
					ctx.SetStatus(PluginStatus.Faulted);
				}
			}
		}
	}
	public async Task DisablePluginAsync(string pluginId, CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		if (_enabledPluginIds.Remove(pluginId))
		{
			await SaveEnabledPluginsConfigurationAsync(cancellationToken);
			if (_plugins.TryGetValue(pluginId, out var ctx) && ctx.Instance != null)
			{
				try
				{
					ctx.Instance.OnDisable();
					ctx.SetStatus(PluginStatus.Disabled);
				}
				catch (Exception ex)
				{
					Logger.LogError(ex, "Error occurred while disabling plugin '{PluginId}'.\n{ExceptionString}", pluginId, ex.ToString());
					ctx.SetStatus(PluginStatus.Faulted);
				}
			}
		}
	}
	public bool ValidateDependencies(string pluginId, out IEnumerable<string> missingDependencies)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		var missing = new List<string>();
		missingDependencies = missing;

		if (!_plugins.TryGetValue(pluginId, out var ctx))
		{
			missing.Add($"Plugin '{pluginId}' itself not found.");
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
				missing.Add($"Dependency '{dep.PluginId}' requires version '{dep.VersionConstraint}' but found '{depCtx.Info.Version}'.");
			}
		}
		return missing.Count == 0;
	}
	private async Task LoadSinglePluginAsync(string pluginId, CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		if (!_plugins.TryGetValue(pluginId, out var ctx) || ctx.IsLoaded)
		{
			return;
		}
		
		var locker = _pluginLocks.GetOrAdd(pluginId, _ => new SemaphoreSlim(1, 1));
		await locker.WaitAsync(cancellationToken);

		try
		{
			if (ctx.IsLoaded) return;

			if (!ValidateDependencies(pluginId, out var missingDeps))
			{
				ctx.SetStatus(PluginStatus.Faulted);
				string depList = string.Join(", ", missingDeps);
				Logger.LogError("Cannot load plugin '{PluginId}'. Missing dependencies: {Dependencies}", pluginId, depList);
				return;
			}

			ctx.SetStatus(PluginStatus.Loading);

			var alc = new PluginAssemblyLoadContext(ctx.Info.InstallPath);
			var assembly = alc.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(ctx.EntryAssembly)));

			var pluginBaseType = typeof(AFSBasePlugin);
			var pluginTypes = assembly.GetTypes()
				.Where(t => pluginBaseType.IsAssignableFrom(t) && !t.IsAbstract)
				.ToList();
			if (pluginTypes.Count == 0)
			{
				throw new InvalidOperationException($"No valid plugin class found in assembly {ctx.EntryAssembly}.");
			}

			if (pluginTypes.Count > 1)
			{
				throw new InvalidOperationException($"Duplicate plugin classes found in assembly {ctx.EntryAssembly}. Only one plugin class is allowed to represent the plugin.");
			}

			var host = (Application.Current as IHostingAvaloniaDesktopApp)!.Host;

			var pluginType = pluginTypes.Single();
			var pluginInstance = ActivatorUtilities.CreateInstance(host.Services, pluginType) as AFSBasePlugin;
			if (pluginInstance == null)
			{
				throw new InvalidOperationException($"Failed to create an instance of plugin class {pluginType.FullName} in assembly {ctx.EntryAssembly}.");
			}
			
			var extI18nLocators = assembly.GetTypes()
				.Where(t => typeof(II18nExtensibleLocalizationLocator).IsAssignableFrom(t) && !t.IsAbstract)
				.ToList();
			if (extI18nLocators.Count > 0)
			{
				var extI18nLocatorInstances = extI18nLocators.Select(l => ActivatorUtilities.CreateInstance(host.Services, l)
						as II18nExtensibleLocalizationLocator)
					.Where(l => l != null)
					.Cast<II18nExtensibleLocalizationLocator>()
					.ToList();
				if (extI18nLocatorInstances.Count > 0)
				{
					_pluginI18nLocators.TryAdd(pluginId, new ConcurrentBag<II18nExtensibleLocalizationLocator>(extI18nLocatorInstances!));
					extI18nLocatorInstances.ForEach(_i18nExtManager.RegisterLocator);
				}
			}
			
			// pluginInstance.PluginId = ctx.Info.Id; (bypass { get; } limitation)
			typeof(AFSBasePlugin).GetField("<PluginId>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(pluginInstance, ctx.Info.Id);
			
			// pluginInstance.EnsureDataDirectory(_pathOptions.PluginPath); (bypass protected method limitation)
			typeof(AFSBasePlugin).GetMethod("EnsureDataDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(pluginInstance, [_pathOptions.PluginPath]);
			
			// Here we don't need to call OnEnable, and the correct method work as 'OnLoaded' is the constructor, which was already been called by the DI container.
			// await Task.Run(() => pluginInstance.OnEnable(), cancellationToken);
			
			ctx.SetLoadContext(alc);
			ctx.RegisterInstance(pluginInstance);
			ctx.SetStatus(PluginStatus.Enabled);
			PluginLoaded?.Invoke(this, new PluginEventArgs(pluginId, PluginStatus.Enabled));
		}
		catch (Exception ex)
		{
			ctx.SetStatus(PluginStatus.Faulted);
			Logger.LogError(ex, "Failed to load plugin '{PluginId}'.\n{ExceptionString}", pluginId, ex.ToString());
		}
		finally
		{
			locker.Release();
		}
	}
	private IEnumerable<PluginInfo> GetPluginsInDependencyOrder()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		var sorted = new List<PluginInfo>();
		var visited = new HashSet<string>();
		var visiting = new HashSet<string>(); // to detect cycle-dependency
		var pluginsToProcess = _plugins.Values.Where(p => p.Info.Status == PluginStatus.Enabled);

		foreach (var context in pluginsToProcess)
		{
			if (!visited.Contains(context.Info.Id))
			{
				TopologicalSortIterative(context, visited, visiting, sorted);
			}
		}
		
		return sorted;
	}
	private void TopologicalSortIterative(PluginContext startContext, HashSet<string> visited, HashSet<string> visiting, List<PluginInfo> sorted)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		// based on Depth-First Search (DFS) Topological Sort algorithm
		var stack = new Stack<(PluginContext ctx, bool processed)>();
		stack.Push((startContext, false));

		while (stack.Count > 0)
		{
			(var ctx, bool processed) = stack.Pop();
			string id = ctx.Info.Id;

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

			stack.Push((ctx, true)); // mark as "process me after process all dependencies"

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
						if (!visited.Contains(depContext.Info.Id))
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
	private void LoadEnabledPluginsConfiguration()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		try
		{
			if (File.Exists(_enabledConfigFile))
			{
				string json = File.ReadAllText(_enabledConfigFile, Encoding.UTF8);
				_enabledPluginIds = JsonConvert.DeserializeObject<ConcurrentHashSet<string>>(json) ?? [];
			}
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Failed to load enabled plugins configuration from {ConfigFile}. Starting with no enabled plugins.\n{ExceptionString}", _enabledConfigFile, ex.ToString());
		}
	}
	private async Task SaveEnabledPluginsConfigurationAsync(CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		try
		{
			string json = JsonConvert.SerializeObject(_enabledPluginIds, Formatting.Indented);
			await File.WriteAllTextAsync(_enabledConfigFile, json, Encoding.UTF8, cancellationToken);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Failed to save enabled plugins configuration to {ConfigFile}.\n{ExceptionString}", _enabledConfigFile, ex.ToString());
		}
	}

	public void Dispose()
	{
		if (_isDisposed) return;
		try
		{
			foreach (string pluginId in _plugins.Keys.ToList())
			{
				try
				{
					if (_plugins.TryGetValue(pluginId, out var ctx) && ctx is {IsLoaded: true})
					{
						var task = UnloadPluginAsync(pluginId);
						if (!task.Wait(TimeSpan.FromSeconds(10)))
						{
							Logger.LogWarning("Timeout while unloading plugin '{PluginId}' during PluginManager disposal.", pluginId);
							ctx.Unload(); // force unload
						}
					}
				}
				catch (AggregateException ex) when (ex.InnerExceptions.Count == 1)
				{
					Logger.LogError(ex.InnerException, "Error occurred while unloading plugin '{PluginId}' during PluginManager disposal.\n{ExceptionString}", pluginId, ex.InnerException!.ToString());
				}
				catch (Exception ex)
				{
					Logger.LogError(ex, "Error occurred while unloading plugin '{PluginId}' during PluginManager disposal.\n{ExceptionString}", pluginId, ex.ToString());
				}
			}
		}
		finally
		{
			_pluginLocks.Clear();
			_plugins.Clear();
			_isDisposed = true;
			GC.SuppressFinalize(this);
		}
	}
	
	public async ValueTask DisposeAsync()
	{
		if (_isDisposed) return;
		try
		{
			foreach (string pluginId in _plugins.Keys.ToList())
			{
				try
				{
					if (_plugins.TryGetValue(pluginId, out var ctx) && ctx.IsLoaded)
					{
						var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
						await UnloadPluginAsync(pluginId, cts.Token);
					}
				}
				catch (Exception ex)
				{
					Logger.LogError(ex, "Error occurred while unloading plugin '{PluginId}' during PluginManager disposal.\n{ExceptionString}", pluginId, ex.ToString());
				}
			}
		}
		finally
		{
			_pluginLocks.Clear();
			_plugins.Clear();
			_isDisposed = true;
			GC.SuppressFinalize(this);
		}
	}
}