// ReSharper disable ClassNeverInstantiated.Global // Are you serious?

using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ProjectAFS.Core;
using ProjectAFS.Core.Abstracts.Services.Shell;
using ProjectAFS.Core.Services.Startup;
using ProjectAFS.Shell.Desktop.UI.Windows;

namespace ProjectAFS.Shell.Desktop;

public sealed class AFSShellManager : IWindowManager
{
	private readonly AFSApp _app;
	private readonly IApplicationLifetime _lifetime;
	private readonly StartupProgressProxy _progress;
	private bool isDisposed;
	
	public AFSShellManager(AFSApp app, IApplicationLifetime lifetime, StartupProgressProxy progress)
	{
		_app = app;
		_lifetime = lifetime;
		_progress = progress;
		isDisposed = false;
	}
	
	public void CompleteToMainUI()
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		Dispatcher.UIThread.Invoke(() =>
		{
			var splash = _progress.Splash;
			splash.ShouldClose = true;
			splash.Close();
			var welcome = _app.CreateInstanceWithInjection<WinWelcome>();
			welcome.Show();
		});
	}

	public void Dispose()
	{
		if (isDisposed) return;
		isDisposed = true; // here we just set the flag, no unmanaged resources to dispose
	}
}