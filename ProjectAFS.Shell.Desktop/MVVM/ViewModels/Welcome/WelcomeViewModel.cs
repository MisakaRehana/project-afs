
// ReSharper disable MemberCanBeProtected.Global

using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ProjectAFS.Core;
using ProjectAFS.Shell.Desktop.UI.Windows;

namespace ProjectAFS.Shell.Desktop.MVVM.ViewModels.Welcome;

public partial class WelcomeViewModel : ObservableObject
{
	private readonly AFSApp? _app;
	private readonly WinWelcome? _window;
	private readonly Dictionary<string, ObservableObject> _pages = new();
	
	[ObservableProperty]
	private ObservableObject _currentPage;

	public WelcomeViewModel(AFSApp? app, WinWelcome? window)
	{
		_app = app;
		_window = window;
		_currentPage = app?.CreateInstanceWithInjection<PgeHomeViewModel>(this)!;
		_pages[typeof(PgeHomeViewModel).FullName!] = _currentPage; // Register initial page
	}
	
	public void NavigateToPage<TPageVM>() where TPageVM : ObservableObject
	{
		if (Design.IsDesignMode) return; // Use design-time data in axaml designer
		string pageVMType = typeof(TPageVM).FullName!;
		if (_pages.TryGetValue(pageVMType, out var page))
		{
			CurrentPage = page;
		}
		else
		{
			var pageVMInstance = _app!.CreateInstanceWithInjection<TPageVM>(this);
			_pages[pageVMType] = pageVMInstance;
			CurrentPage = pageVMInstance;
		}
	}

	public virtual void ExecuteSkipWelcome()
	{
		Dispatcher.UIThread.Invoke(() =>
		{
			_window!.ShouldShutdown = false;
			_window.Close();
			var main = _app!.CreateInstanceWithInjection<WinMain>();
			main.Show();
		});
	}
}

public sealed class DesignWelcomeViewModel : WelcomeViewModel
{
	public DesignWelcomeViewModel() : base(null!, null!)
	{
		CurrentPage = new DesignPgeHomeViewModel();
	}

	public override void ExecuteSkipWelcome()
	{
		// No operation in design mode
	}
}