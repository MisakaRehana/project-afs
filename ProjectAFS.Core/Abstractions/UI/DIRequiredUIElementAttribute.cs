using Microsoft.Extensions.DependencyInjection;

namespace ProjectAFS.Core.Abstractions.UI;

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