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
	public virtual void Prev()
	{
		
	}


	[RelayCommand(CanExecute = nameof(CanGoNext))]
	public virtual void Next()
	{
		
	}
	
	protected void NavigateToPage<TPageVM>() where TPageVM : ObservableObject
	{
		if (Design.IsDesignMode) return;
		_frame.NavigateToPage<TPageVM>();
	}
}