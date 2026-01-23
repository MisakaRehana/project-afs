using Avalonia.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.UI;
using IApplicationLifetime = Avalonia.Controls.ApplicationLifetimes.IApplicationLifetime;

namespace ProjectAFS.Core.Services.Startup;

public sealed class StartupHostService : IHostedService
{
	private readonly AFSApp _app;
	private readonly StartupProgressProxy _progress;

	public StartupHostService(AFSApp app, StartupProgressProxy progress)
	{
		_app = app;
		_progress = progress;
	}
	
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await Dispatcher.UIThread.InvokeAsync(() =>
		{
			var splash = _app.CreateInstanceWithInjection<WinSplash>();
			splash.Show();
			_progress.Attach(splash);
		});
		await Task.Delay(TimeSpan.FromSeconds(3.5f), cancellationToken);
	}
	
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		await Task.CompletedTask;
	}
}