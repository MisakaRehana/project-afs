using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProjectAFS.Core;
using ProjectAFS.Shell.Desktop.MVVM.ViewModels.Welcome;

namespace ProjectAFS.Shell.Desktop.UI.Windows;

public partial class WinWelcome : Window
{
	public bool ShouldShutdown { get; set; }

	private readonly AFSApp _app;
	private bool blockClosing;
	
	// for Designer only
	public WinWelcome()
	{
		if (!Design.IsDesignMode)
		{
			throw new InvalidOperationException("This constructor is for design-time only.");
		}
		InitializeComponent();
		_app = null!;
		// DataContext = new DesignWelcomeViewModel(); // moved to axaml design data
		ShouldShutdown = true;
		blockClosing = true;
	}

	[ActivatorUtilitiesConstructor]
	public WinWelcome(AFSApp app)
	{
		InitializeComponent();
		_app = app;
		_app.BindAsMainWindow(this);
		DataContext = _app.CreateInstanceWithInjection<WelcomeViewModel>(this);
		ShouldShutdown = true;
		blockClosing = true;
	}

	private void Window_OnClosing(object? sender, WindowClosingEventArgs args)
	{
		if (!blockClosing) return;
		if (!ShouldShutdown) return;
		args.Cancel = true;
		Closing -= Window_OnClosing;
		blockClosing = false;
		_app.Shutdown();
	}
}