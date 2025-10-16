using Avalonia.Controls.ApplicationLifetimes;

namespace ProjectAFS.Core.Abstractions;

/// <summary>
/// Represents an Avalonia desktop application that is hosted within a generic host.
/// </summary>
public interface IHostingAvaloniaDesktopApp : IHostingAvaloniaApp
{
	/// <summary>
	/// Gets the desktop application lifetime instance that provides events and properties specific to desktop applications.
	/// </summary>
	public IClassicDesktopStyleApplicationLifetime DesktopAppLifetime { get; }

	/// <summary>
	/// Starts the Avalonia desktop application with the specified command-line arguments.
	/// </summary>
	/// <param name="args">The command-line arguments to pass to the application.</param>
	/// <returns>Returns an integer exit code indicating the result of the application run.</returns>
	public int Run(string[] args);
}