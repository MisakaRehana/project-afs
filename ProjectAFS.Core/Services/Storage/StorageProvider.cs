using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstracts.Services.Storage;
using ProjectAFS.Core.Models.Startup;
using ProjectAFS.Core.Services.Startup;
using ProjectAFS.Core.Utility.Threading;

namespace ProjectAFS.Core.Services.Storage;

public sealed class StorageService : IStorageService
{
	private readonly AFSApp _app;
	private readonly ILogger<StorageService> _logger;
	private readonly StartupProgressProxy _progress;
	
	public StorageService(AFSApp app, ILogger<StorageService> logger, StartupProgressProxy progress)
	{
		_app = app;
		_logger = logger;
		_progress = progress;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await _progress.ReportAsync(new StartupProgressReport("splash.init.core.storage", StartupStage.CoreServicesInitialization), cancellationToken);
		await AFSTask.CompletedTask;
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		await AFSTask.CompletedTask;
	}

	public async AFSTask<IEnumerable<string>> SelectFolderAsync(string? initialPath = null, string? title = null, bool allowMultiSelect = false)
	{
		return await Dispatcher.UIThread.InvokeAsync(async () =>
		{
			var topLevel = _app.MainWindow;
			if (topLevel == null) return Enumerable.Empty<string>();

			var provider = topLevel.StorageProvider;

			IStorageFolder? startLocation = null;
			if (!string.IsNullOrEmpty(initialPath))
			{
				startLocation = await provider.TryGetFolderFromPathAsync(initialPath);
			}

			var r = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = title ?? "Select Folder",
				AllowMultiple = allowMultiSelect,
				SuggestedStartLocation = startLocation
			});
			
			// Use .TryGetLocalPath() to get a friendly path string, as x.Path is a Uri.
			return r.Select(x => x.TryGetLocalPath() ?? x.Path.LocalPath);
		});
	}
}