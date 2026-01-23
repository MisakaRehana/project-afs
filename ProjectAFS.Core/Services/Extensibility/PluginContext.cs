using ProjectAFS.Core.Abstracts.Services.Extensibility;
using ProjectAFS.Core.Models.Extensibility;

namespace ProjectAFS.Core.Services.Extensibility;

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