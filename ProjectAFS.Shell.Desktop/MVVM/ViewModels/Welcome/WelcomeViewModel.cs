// ReSharper disable MemberCanBeProtected.Global

using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ProjectAFS.Core;
using ProjectAFS.Shell.Desktop.UI.Windows;

namespace ProjectAFS.Shell.Desktop.MVVM.ViewModels.Welcome;

public partial class WelcomeViewModel : ObservableObject, IDisposable
{
	private readonly AFSApp? _app;
	private readonly WinWelcome? _window;
	private readonly Dictionary<string, ObservableObject> _pages = new();
	
	[ObservableProperty]
	private ObservableObject _currentPage;

	protected bool _isDisposed;

	public WelcomeViewModel(AFSApp? app, WinWelcome? window)
	{
		_isDisposed = false;
		_app = app;
		_window = window;
		_currentPage = app?.CreateInstanceWithInjection<PgeHomeViewModel>(this)!;
		_pages[typeof(PgeHomeViewModel).FullName!] = _currentPage; // Register initial page
	}
	
	public virtual void NavigateToPage<TPageVM>(bool cache = true, params object[] args) where TPageVM : ObservableObject
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		string pageVMType = typeof(TPageVM).FullName!;
		if (!cache || !_pages.TryGetValue(pageVMType, out var page))
		{
			// for !cache situation, dispose last cached page of the same type
			if (_pages.TryGetValue(pageVMType, out var lastPage) && lastPage is IDisposable disposableLastPage)
			{
				disposableLastPage.Dispose();
				_pages.Remove(pageVMType);
			}
			var pageVMInstance = _app!.CreateInstanceWithInjection<TPageVM>([this, ..args]);
			_pages[pageVMType] = pageVMInstance;
			CurrentPage = pageVMInstance;
		}
		else
		{
			CurrentPage = page;
		}
	}

	public virtual void ExecuteSkipWelcome()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		Dispatcher.UIThread.Invoke(() =>
		{
			_window!.ShouldShutdown = false;
			_window.Close();
			var main = _app!.CreateInstanceWithInjection<WinMain>();
			main.Show();
		});
	}
	
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	
	protected virtual void Dispose(bool disposing)
	{
		if (_isDisposed) return;
		if (disposing)
		{
			foreach (var page in _pages.Values)
			{
				if (page is IDisposable disposablePage)
				{
					disposablePage.Dispose();
				}
			}
			_pages.Clear();
		}
		_isDisposed = true;
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