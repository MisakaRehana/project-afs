using ProjectAFS.Core.Abstracts.Services.Shell;
using ProjectAFS.Core.Abstracts.Services.Startup;

namespace ProjectAFS.Core.Services.Startup;

public sealed class StartupBridge : IWindowManager, IStartupBridge
{
	private readonly StartupProgressProxy _progress;
	private IWindowManager? _shell;
	private bool isDisposed;

	public StartupBridge(StartupProgressProxy progress)
	{
		_progress = progress;
		isDisposed = false;
	}

	public void RegisterShell<T>(T shell) where T : class, IWindowManager
	{
		_shell = shell;
	}

	public void CompleteToMainUI()
	{
		if (_shell == null)
		{
			throw new InvalidOperationException("Shell is not registered.");
		}
		_shell.CompleteToMainUI();
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
				_shell?.Dispose();
			}
			
			// Dispose unmanaged resources here if any
			
			isDisposed = true;
		}
	}
	
	~StartupBridge()
	{
		Dispose(false);
	}
}