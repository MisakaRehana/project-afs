using Avalonia.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.UI;
using ProjectAFS.Core.Utility.Threading;
using IApplicationLifetime = Avalonia.Controls.ApplicationLifetimes.IApplicationLifetime;

namespace ProjectAFS.Core.Services.Startup;

public sealed class StartupHostService : IHostedService
{
	private readonly StartupProgressProxy _progress;
	private readonly ILogger<WinSplash> _loggerSplash;
	private readonly II18nService _i18n;
	private readonly IApplicationLifetime _lifetime;
	
	public StartupHostService(StartupProgressProxy progress, ILogger<WinSplash> loggerSplash, II18nService i18n, IApplicationLifetime lifetime)
	{
		_progress = progress;
		_loggerSplash = loggerSplash;
		_i18n = i18n;
		_lifetime = lifetime;
	}
	
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await Dispatcher.UIThread.InvokeAsync(() =>
		{
			var splash = new WinSplash(_lifetime, _loggerSplash, _i18n);
			splash.Show();
			_progress.Attach(splash);
			// then, other startup services can use _progress to report progress with _progress.Report(...) later.
		});
	}
	
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		await Task.CompletedTask; // stop logic is processed in WinMain [Desktop] or ActMain [Mobile]
	}
}