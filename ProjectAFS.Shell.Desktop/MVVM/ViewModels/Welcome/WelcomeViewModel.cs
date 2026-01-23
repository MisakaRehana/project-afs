using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProjectAFS.Core;
using ProjectAFS.Shell.Desktop.MVVM.Bindings.Welcome;
using ProjectAFS.Shell.Desktop.UI.Windows;
// ReSharper disable MemberCanBeProtected.Global

namespace ProjectAFS.Shell.Desktop.MVVM.ViewModels.Welcome;

public partial class WelcomeViewModel : ObservableObject
{
	private readonly AFSApp _app;
	private readonly WinWelcome _window;
	
	[ObservableProperty]
	private ObservableCollection<RecentProjectItem> recentProjects = [];

	public WelcomeViewModel(AFSApp app, WinWelcome window)
	{
		_app = app;
		_window = window;
	}
	
	[RelayCommand]
	public virtual void SkipWelcome()
	{
		Dispatcher.UIThread.Invoke(() =>
		{
			_window.ShouldShutdown = false;
			_window.Close();
			var main = _app.CreateInstanceWithInjection<WinMain>();
			main.Show();
		});
	}
}

public sealed partial class DesignWelcomeViewModel : WelcomeViewModel
{
	public DesignWelcomeViewModel() : base(null!, null!)
	{
		RecentProjects =
		[
			new RecentProjectItem() {Title = "Sample Project 1", SolutionFilePath = @"C:\Path\To\SampleProject1.afsln"},
			new RecentProjectItem() {Title = "Sample Project 2", SolutionFilePath = @"C:\Path\To\SampleProject2.afsln"},
			new RecentProjectItem() {Title = "Sample Project 3", SolutionFilePath = @"C:\Path\To\SampleProject3.afsln"},
			new RecentProjectItem() {Title = "Sample Project 4", SolutionFilePath = @"C:\Path\To\SampleProject4.afsln"}
		];
	}
	
	public override void SkipWelcome()
	{
		// No operation in design mode
	}
}