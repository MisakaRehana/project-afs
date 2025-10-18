using Avalonia.Controls;

namespace ProjectAFS.Core.Abstractions.UI;

public abstract class DesignableAvaloniaWindow<TViewModel> : Window where TViewModel : class, new()
{
	protected void SetupDataContext()
	{
		DataContext = Design.IsDesignMode ? CreateDesignTimeViewModel() : CreateRuntimeViewModel();
	}

	protected virtual TViewModel CreateDesignTimeViewModel()
	{
		return new TViewModel();
	}
	
	protected abstract TViewModel CreateRuntimeViewModel();
}