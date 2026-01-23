using Avalonia;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core;
using ProjectAFS.Core.Abstracts.Services.Extensibility;
using ProjectAFS.Core.Abstracts.Services.Startup;
using ProjectAFS.Core.Services.Startup;

namespace ProjectAFS.Shell.Desktop;

public sealed class AFSShellPlugin : IPlugin
{
	private readonly ILogger<AFSShellPlugin> _logger;
	private readonly AFSShellManager _shellManager;
	private bool isDisposed;
	
	public AFSShellPlugin(ILogger<AFSShellPlugin> logger, IStartupBridge bridge, StartupProgressProxy progress)
	{
		var app = (Application.Current as AFSApp)!;
		_logger = logger;
		_shellManager = app.CreateInstanceWithInjection<AFSShellManager>(progress);
		bridge.RegisterShell(_shellManager);
		_logger.LogDebug("Registered AFS Shell Manager with Startup Bridge.");
		isDisposed = false;
	}
	
	public void OnEnable()
	{
		_logger.LogDebug("AFS Shell Plugin enabled.");
	}

	public void OnDisable()
	{
		_logger.LogDebug("AFS Shell Plugin disabled.");
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
				_shellManager?.Dispose();
			}
			
			// Dispose unmanaged resources here if any
			
			isDisposed = true;
		}
	}

	~AFSShellPlugin()
	{
		Dispose(false);
	}
}