using System.Reflection;
using System.Runtime.Loader;
using ProjectAFS.Core.Utility.SharpCompress;
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