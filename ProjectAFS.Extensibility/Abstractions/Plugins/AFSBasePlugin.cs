using Microsoft.Extensions.Logging;
using ProjectAFS.Extensibility.Abstractions.Configuration;

namespace ProjectAFS.Extensibility.Abstractions.Plugins;

/// <summary>
/// Represents the base class for all project-afs plugins.<br />
/// Inherit from this class to create your own plugin.<br />
/// To use automatic dependency injection, add a constructor with the required services as parameters.<br />
/// See https://docs.misakacastle.moe/project-afs/fundamentals/plugins for more information.
/// </summary>
public abstract class AFSBasePlugin : IDisposable
{
	protected string PluginId { get; } = null!;
	
	private bool _disposed;

	/// <summary>
	/// Instantiate a new instance of <see cref="AFSBasePlugin"/>.<br />
	/// Called when the plugin is loaded, regardless of whether it is enabled.
	/// </summary>
	protected AFSBasePlugin()
	{
		
	}
	
	protected void EnsureDataDirectory(string basePath)
	{
		var dataDir = Path.Combine(basePath, PluginId);
		if (!Directory.Exists(dataDir))
		{
			Directory.CreateDirectory(dataDir);
		}
	}
	
	/// <summary>
	/// Called when the plugin is enabled.<br />
	/// Notice: This method won't be called when the plugin is loaded, which is theoretically not allow plugin to handle the loaded event.
	/// </summary>
	public virtual void OnEnable() { }
	
	/// <summary>
	/// Called when the plugin is disabled.
	/// </summary>
	public virtual void OnDisable() { }

	/// <summary>
	/// Core Dispose logic for derived classes. <br />
	/// Only being called when the IDE is shutting down and the plugin had been loaded before.
	/// </summary>
	/// <param name="disposing">
	/// <see langword="true"/> if called from <see cref="Dispose()"/>; <see langword="false"/> if called from finalizer.<br />
	/// Best practice is to ONLY release unmanaged resources when <see langword="false"/> if being overridden.
	/// </param>
	protected virtual void Dispose(bool disposing)
	{
		if (_disposed) return;
		_disposed = true;
	}
	
	/// <summary>
	/// Only being called when the IDE is shutting down and the plugin had been loaded before.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Only being called when the IDE is shutting down and the plugin had been loaded before. (Finalizer)
	/// </summary>
	~AFSBasePlugin()
	{
		Dispose(false);
	}
}