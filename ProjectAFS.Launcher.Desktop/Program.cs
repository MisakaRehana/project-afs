using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NMsgBox = ProjectAFS.Core.Utility.Native.NativeMessageBox;

namespace ProjectAFS.Launcher.Desktop;

public static class Program
{
	[STAThread]
	public static async Task<int> Main(string[] args)
	{
		var builder = Host.CreateDefaultBuilder(args);
		builder.ConfigureServices(Startup.ConfigureServices);
		using var host = builder.Build();

		try
		{
			await host.StartAsync();
			var app = host.Services.GetRequiredService<AFSApp>();
			int exitCode = app.Run(args);
			if (exitCode != 0)
			{
				NMsgBox.Show("An unexpected fatal error occurred and caused the application to crash. " +
				             "Please consider any recent changes you made (e.g., installing a plugin) that might have caused this issue " +
				             "(See https://docs.misakacastle.moe/project-afs/troubleshooting for more information). " +
				             "If this is not a known issue, please file a bug. " +
				             "This is an unrecoverable error and application will be shut down.",
					"Fatal Error - project-afs", NMsgBox.MessageBoxButtons.OK, NMsgBox.MessageBoxIcon.Error);
			}
			return exitCode;
		}
		catch (Exception ex)
		{
			NMsgBox.Show("An unexpected error occurred:\n" + ex, "Fatal Error", NMsgBox.MessageBoxButtons.OK, NMsgBox.MessageBoxIcon.Error);
			return 1;
		}
		finally
		{
			await host.StopAsync();
		}
	}

	// ReSharper disable once UnusedMember.Global // for Design time preview only. We don't use this in runtime. See ProjectAFS.Launcher.Desktop.AFSApp.Run() for more information.
	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<AFSApp>()
			.UsePlatformDetect()
			.LogToTrace();
	}
}