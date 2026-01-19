using Avalonia.Threading;
using ProjectAFS.Core.Models.Startup;
using ProjectAFS.Core.UI;

namespace ProjectAFS.Core.Services.Startup;

public sealed class StartupProgressProxy : IProgress<StartupProgressProxy>
{
	private WinSplash? _splash;

	public void Attach(WinSplash splash)
	{
		_splash = splash;
	}

	public void Report(StartupProgressReport value)
	{
		Dispatcher.UIThread.Post(() =>
		{
			if (_splash == null) return;
			_splash.UpdateProgress(value);
		});
	}
	
	public void Report(StartupProgressProxy value)
	{
		// No implementation needed
	}
}