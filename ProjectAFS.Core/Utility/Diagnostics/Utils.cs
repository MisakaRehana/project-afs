using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstractions;

namespace ProjectAFS.Core.Utility.Diagnostics;

/// <summary>
/// Represents utility methods for diagnostics and error handling.
/// </summary>
public class Utils
{
	/// <summary>
	/// Forcefully crashes the application by logging the exception and shutting down the application with the specified exit code.
	/// </summary>
	/// <param name="ex">The exception that caused the crash.</param>
	/// <param name="exitCode">The exit code to use when shutting down the application. Default is -1.</param>
	/// <remarks>This method will not return; it will terminate the application.</remarks>
	public static void ForceCrash(Exception ex, int exitCode = -1)
	{
		var app = Application.Current as IHostingAvaloniaApp;
		if (app is not Application {ApplicationLifetime: IClassicDesktopStyleApplicationLifetime desktopApp}) return;
		var logger = app.Host.Services.GetService<ILogger<Utils>>();
		logger?.LogCritical(ex, "Crash!!\n\n" +
		                        "Happened at {DateTime} UTC\n" +
		                        "Stacktrace:\n{StackTrace}\n\n" +
		                        "{Type}: {Message}",
			DateTimeOffset.UtcNow, ex.StackTrace, ex.GetType().FullName, ex.Message);
		desktopApp.Shutdown(exitCode);
	}
}