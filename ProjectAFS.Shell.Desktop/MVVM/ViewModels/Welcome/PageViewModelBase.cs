using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProjectAFS.Shell.Desktop.MVVM.ViewModels.Welcome;

public abstract partial class PageViewModelBase : ObservableObject
{
	[ObservableProperty]
	private bool canGoNext;
	
	protected readonly WelcomeViewModel _frame;

	protected PageViewModelBase(WelcomeViewModel frame)
	{
		_frame = frame;
	}
	
	[RelayCommand]
	protected virtual void Prev()
	{
		
	}


	[RelayCommand(CanExecute = nameof(CanGoNext))]
	protected virtual void Next()
	{
		
	}
	
	protected void NavigateToPage<TPageVM>(bool cache = true, params object[] args) where TPageVM : ObservableObject
	{
		if (Design.IsDesignMode) return;
		_frame.NavigateToPage<TPageVM>(cache, args);
	}
}