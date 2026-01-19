using System;
using Avalonia;
using ProjectAFS.Core;

namespace ProjectAFS.Launcher.Desktop;

public static class Program
{
	[STAThread]
	public static int Main(string[] args)
	{
		var builder = AppBuilder.Configure<AFSApp>()
			.UsePlatformDetect()
			.LogToTrace();
		return builder.RunDesktop(args);
	}
	
	// provided for Avalonia Designer only.
	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<AFSApp>()
			.UsePlatformDetect()
			.LogToTrace();
	}
}