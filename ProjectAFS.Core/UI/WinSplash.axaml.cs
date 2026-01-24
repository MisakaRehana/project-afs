// ReSharper disable ClassNeverInstantiated.Global
using System.Reflection;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.Models.Startup;
using ProjectAFS.Core.Utility.Strings;
using ProjectAFS.Core.Utility.Threading;
using IApplicationLifetime = Avalonia.Controls.ApplicationLifetimes.IApplicationLifetime;

namespace ProjectAFS.Core.UI;

public sealed partial class WinSplash : Window
{
    public bool ShouldClose { get; set; }
    private readonly AFSApp _app;
	private readonly IApplicationLifetime? _lifetime;
	private readonly ILogger<WinSplash> _logger;
	private readonly II18nService _i18n;
	private int? _stageCount;

	public WinSplash()
	{
#pragma warning disable AFS0001
		_app = (AFSApp)Application.Current!;
#pragma warning restore AFS0001
        _lifetime = Application.Current?.ApplicationLifetime;
		_logger = new LoggerFactory().CreateLogger<WinSplash>();
		_i18n = null!; // for design time only
		InitializeComponent();
	}
	
	[ActivatorUtilitiesConstructor]
	public WinSplash(AFSApp app, IApplicationLifetime lifetime, ILogger<WinSplash> logger, II18nService i18n)
	{
		_app = app;
		_lifetime = lifetime;
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
		try
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
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred during splash screen fade-in.");
			if (typeof(AFSApp).GetField("_host", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_app) is IHost host)
			{
				await host.StopAsync(); // stop the host.
			}

			if (_lifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				desktop.Shutdown(1); // exit with error code 1
			}
		}
	}
	
	private void OnClosing(object? sender, WindowClosingEventArgs args)
	{
		if (!ShouldClose)
		{
			_logger.LogDebug("Splash close operation was attempted but blocked.");
			args.Cancel = true; // cancel the close operation (this will prevent Alt+F4 or window close button from closing the splash)
		}
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
			
			_stageCount ??= Enum.GetValues<StartupStage>().Length;
			float progress = (float)((int)report.current + 1) / _stageCount.Value;
			Pgbr_Loading.Value = progress;
			string template = report.isAutoLocalized ? report.Message : _i18n[report.Message].ToString();
			Tbk_StatusText.Text = StringExtension.AdvancedFormat(template, (int)report.current + 1, _stageCount);
		});
	}
}