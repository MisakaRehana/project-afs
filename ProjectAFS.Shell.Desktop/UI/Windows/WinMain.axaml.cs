using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using ProjectAFS.Core;
using ProjectAFS.Shell.Desktop.MVVM.ViewModels.Main;

namespace ProjectAFS.Shell.Desktop.UI.Windows;

public partial class WinMain : Window
{
	private readonly AFSApp _app;
	
	public WinMain()
	{
		if (!Design.IsDesignMode)
		{
			throw new InvalidOperationException("This constructor is for design-time only.");
		}
		InitializeComponent();
		_app = null!;
		// DataContext = new DesignMainViewModel(); moved to axaml design data
	}
	
	[ActivatorUtilitiesConstructor]
	public WinMain(AFSApp app)
	{
		InitializeComponent();
		_app = app;
		DataContext = _app.CreateInstanceWithInjection<MainViewModel>();
	}
	
	private void Window_OnClosing(object? sender, WindowClosingEventArgs args)
	{
		if ((DataContext as MainViewModel)?.TryExitApplication() == true)
		{
			Closing -= Window_OnClosing;
			(DataContext as MainViewModel)?.ExitApplicationCommand.Execute(null);
		}
		args.Cancel = true;
	}

	private void Mdi_File_Exit_OnClick(object? sender, RoutedEventArgs args)
	{
		(DataContext as MainViewModel)?.ExitApplicationCommand.Execute(null);
	}
}