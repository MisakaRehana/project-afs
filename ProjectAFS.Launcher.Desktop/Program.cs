using System;
using Avalonia;
using ProjectAFS.Core;
using ProjectAFS.Shell.Desktop;

namespace ProjectAFS.Launcher.Desktop;

public static class Program
{
	[STAThread]
	public static int Main(string[] args)
	{
		var shell = typeof(AFSShellPlugin);
		
		var builder = AppBuilder.Configure(() => new AFSApp(shell))
			.UsePlatformDetect()
			.LogToTrace();
		return builder.RunDesktop(args);
	}
	
	// provided for Avalonia Designer only.
	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure(() => new AFSApp())
			.UsePlatformDetect()
			.LogToTrace();
	}
}