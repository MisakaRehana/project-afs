using Microsoft.Extensions.Hosting;
using ProjectAFS.Core.Abstracts.Services.Shell;
using ProjectAFS.Core.Models.Startup;

namespace ProjectAFS.Core.Services.Startup;

public sealed class StartupCompletionService : IHostedService
{
	private readonly StartupProgressProxy _progress;
	private readonly IWindowManager _windowManager;
	
	public StartupCompletionService(StartupProgressProxy progress, IWindowManager windowManager)
	{
		_progress = progress;
		_windowManager = windowManager;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		_progress.Report(new StartupProgressReport("splash.init.complete", StartupStage.AlmostDone, isAutoLocalized: false));
		await Task.Delay(TimeSpan.FromSeconds(1.5f), cancellationToken); // simulate finalizing delay
		_windowManager.CompleteToMainUI();
	}
	
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		await Task.CompletedTask;
	}
}