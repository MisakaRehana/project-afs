using Avalonia.Threading;
using ProjectAFS.Core.Models.Startup;
using ProjectAFS.Core.UI;
using ProjectAFS.Core.Utility.Threading;

namespace ProjectAFS.Core.Services.Startup;

public sealed class StartupProgressProxy : IProgress<StartupProgressProxy>
{
	public WinSplash Splash => _splash!;
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
	
	public async AFSTask ReportAsync(StartupProgressReport value, CancellationToken cancellationToken = default)
	{
		await AFSTask.Create(async () =>
		{
			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				if (_splash == null) return;
				_splash.UpdateProgress(value);
			});
		}).AttachExternalCancellation(cancellationToken);
	}
	
	public void Report(StartupProgressProxy value)
	{
		// No implementation needed
	}
}