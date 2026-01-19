using Microsoft.Extensions.Hosting;
using ProjectAFS.Core.UI;
using ProjectAFS.Core.Utility.Threading;

namespace ProjectAFS.Core.Services.Startup;

public sealed class StartupHostService : IHostedService
{
	private readonly StartupProgressProxy _progress;
	
	public StartupHostService(StartupProgressProxy progress) // Dependency Injection (see AFSApp class for more information)
	{
		_progress = progress;
	}
	
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		// await Dispatcher.UIThread.InvokeAsync(() =>
		// {
		// 	var splash = new WinSplash();
		// 	splash.Show();
		// 	_progress.Attach(splash);
		// 	// then, other startup services can use _progress to report progress with _progress.Report(...) later.
		// });
		await AFSTask.SwitchToMainThread();
		var splash = new WinSplash();
		splash.Show();
		_progress.Attach(splash);
		// then, other startup services can use _progress to report progress with _progress.Report(...) later.
	}
	
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		await Task.CompletedTask; // stop logic is processed in WinMain [Desktop] or ActMain [Mobile]
	}
}