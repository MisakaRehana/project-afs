using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.Models.Startup;
using ProjectAFS.Core.Utility.Strings;
using ProjectAFS.Core.Utility.Threading;

namespace ProjectAFS.Core.UI;

public sealed partial class WinSplash : Window
{
	private readonly ILogger<WinSplash> _logger;
	private readonly II18nService _i18n;

	public WinSplash()
	{
		_logger = new LoggerFactory().CreateLogger<WinSplash>();
		_i18n = null!; // for design time only
		InitializeComponent();
	}
	
	public WinSplash(ILogger<WinSplash> logger, II18nService i18n) // Dependency Injection
	{
		_logger = logger;
		_i18n = i18n;
		InitializeComponent();
		if (!Design.IsDesignMode)
		{
			Opacity = 0; // splash is fade-in
		}

		Pgbr_Loading.Minimum = 0;
		Pgbr_Loading.Maximum = 1;
		Pgbr_Loading.Value = 0;

		Grd_FooterLoading.IsVisible = false; // default to invisible until at least one progress report is received
		Tbk_StatusText.Text = string.Empty;
	}
	
	private async void OnOpened(object? sender, EventArgs args)
	{
		if (Design.IsDesignMode) return;
		// await AFSTask.Delay(16); // wait a frame to ensure the window is rendered
		// await RunFadeInAsync();
		await Dispatcher.UIThread.InvokeAsync(async () =>
		{
			await AFSTask.Delay(16);
			await RunFadeInAsync();
		});
	}

	private async AFSTask RunFadeInAsync()
	{
		var fadeIn = new Animation()
		{
			Duration = TimeSpan.FromSeconds(0.35),
			FillMode = FillMode.Forward,
			Easing = new CubicEaseInOut(),
			Children =
			{
				new KeyFrame()
				{
					Cue = new Cue(0d),
					Setters = {new Setter(OpacityProperty, 0d)}
				},
				new KeyFrame()
				{
					Cue = new Cue(1d),
					Setters = {new Setter(OpacityProperty, 1d)}
				}
			}
		};
		
		await fadeIn.RunAsync(this);
	}
	
	
	public void UpdateProgress(StartupProgressReport report)
	{
		Dispatcher.UIThread.Invoke(() =>
		{
			if (!Grd_FooterLoading.IsVisible)
			{
				Grd_FooterLoading.IsVisible = true;
			}

			float progress = (float)report.currStepNum / report.totalSteps;
			Pgbr_Loading.Value = progress;
			string template = report.isLocalized ? _i18n[report.Message].ToString() : report.Message;
			Tbk_StatusText.Text = StringExtension.AdvancedFormat(template, report.currStepNum, report.totalSteps);
		});
	}
}