using Microsoft.Extensions.Hosting;

namespace ProjectAFS.Core.Abstractions;

/// <summary>
/// Represents an Avalonia application that is hosted within a generic host.
/// </summary>
public interface IHostingAvaloniaApp
{
	/// <summary>
	/// Gets the generic host instance that manages the application's lifetime and services.
	/// </summary>
	public IHost Host { get; }
}