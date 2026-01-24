// ReSharper disable MemberCanBeProtected.Global
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectAFS.Shell.Desktop.MVVM.Bindings.Welcome;

namespace ProjectAFS.Shell.Desktop.MVVM.ViewModels.Welcome;

public partial class PgeHomeViewModel : PageViewModelBase
{
	
	[ObservableProperty]
	private ObservableCollection<RecentProjectItem> _recentProjects = [];

	public PgeHomeViewModel(WelcomeViewModel frame) : base(frame)
	{
		
	}
	
	[RelayCommand]
	private void SwitchToNewProjectPage()
	{
		NavigateToPage<PgeNewProjectViewModel>();
	}
	
	[RelayCommand]
	private void SkipWelcome()
	{
		Dispatcher.UIThread.Invoke(() =>
		{
			_frame.ExecuteSkipWelcome();
		});
	}
}

public sealed class DesignPgeHomeViewModel : PgeHomeViewModel
{
	public DesignPgeHomeViewModel() : base(null!)
	{
		RecentProjects =
		[
			new RecentProjectItem() {Title = "Sample Project 1", SolutionFilePath = @"C:\Path\To\SampleProject1.afsln"},
			new RecentProjectItem() {Title = "Sample Project 2", SolutionFilePath = @"C:\Path\To\SampleProject2.afsln"},
			new RecentProjectItem() {Title = "Sample Project 3", SolutionFilePath = @"C:\Path\To\SampleProject3.afsln"},
			new RecentProjectItem() {Title = "Sample Project 4", SolutionFilePath = @"C:\Path\To\SampleProject4.afsln"}
		];
	}
}