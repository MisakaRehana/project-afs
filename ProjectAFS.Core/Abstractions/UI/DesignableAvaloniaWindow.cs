using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

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

/// <summary>
/// Represents a UI element that requires dependency injection support.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DIRequiredUIElementAttribute : Attribute
{
	public ServiceLifetime Lifetime { get; }
	
	public DIRequiredUIElementAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient)
	{
		Lifetime = lifetime;
	}
}