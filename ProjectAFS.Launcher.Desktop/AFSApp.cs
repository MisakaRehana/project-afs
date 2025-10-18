using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectAFS.Core.Abstractions;
using ProjectAFS.Core.Abstractions.UI;
using ProjectAFS.Core.Utility.Threading;
using ProjectAFS.Launcher.Desktop.Windows;

namespace ProjectAFS.Launcher.Desktop;

// ReSharper disable once ClassNeverInstantiated.Global // see ProjectAFS.Launcher.Desktop.Startup.ConfigureServices() for more information.
public class AFSApp : Application, IHostingAvaloniaDesktopApp
{
	public IHost Host { get; }
	public IClassicDesktopStyleApplicationLifetime DesktopAppLifetime => (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
	
	// ReSharper disable once UnusedMember.Global // for Design time preview only
	public AFSApp()
	{
		Host = null!;
	}

	// ReSharper disable once UnusedMember.Global // see ProjectAFS.Launcher.Desktop.Startup.ConfigureServices() for more information.
	public AFSApp(IHost host)
	{
		Host = host;
	}

	/// <summary>
	/// Same as WPF's <c>OnStartup</c>.
	/// </summary>
	public override void OnFrameworkInitializationCompleted()
	{
		try
		{
			if (Design.IsDesignMode) // design time will not implement ApplicationLifetime
			{
				return;
			}

			DesktopAppLifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
			DesktopAppLifetime.MainWindow = null;

			var splash = ActivatorUtilities.CreateInstance<WinSplash>(Host.Services);
			splash.Show();
		}
		finally
		{
			base.OnFrameworkInitializationCompleted();
		}
	}

	public int Run(string[] args)
	{
		var builder = AppBuilder.Configure(() => this)
			.UsePlatformDetect()
			.LogToTrace();
		return builder.StartWithClassicDesktopLifetime(args);
	}
}

public static class AppExtensions
{
	public static IServiceCollection AddAvaloniaDesktopApplication<T>(this IServiceCollection services) where T : class, IHostingAvaloniaDesktopApp
	{
		return services.AddSingleton<T>().AddSingleton<IHostingAvaloniaDesktopApp>(provider => provider.GetRequiredService<T>());
	}
	
	public static IServiceCollection AddAFSApp(this IServiceCollection services)
	{
		services.AddAvaloniaDesktopApplication<AFSApp>();
		services.AddAFSAppWindows();
		return services;
	}
	
	private static IServiceCollection AddAFSAppWindows(this IServiceCollection services)
	{
		// services.AddSingleton<WinSplash>();
		// services.AddSingleton<WinMain>();
		// services.AddSingleton<WinLanguageSelect>();
		
		var uiElementTypes = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(t => t.GetTypes())
			.Where(t => t is {IsClass: true, IsAbstract: false, IsPublic: true} && t.GetCustomAttribute<DIRequiredUIElementAttribute>() != null &&
			            (typeof(Control).IsAssignableFrom(t) || typeof(TopLevel).IsAssignableFrom(t)))
			.ToList();

		foreach (var type in uiElementTypes)
		{
			var attr = type.GetCustomAttribute<DIRequiredUIElementAttribute>()!;
			switch (attr.Lifetime)
			{
				case ServiceLifetime.Transient:
					services.AddTransient(type);
					break;
				case ServiceLifetime.Scoped: // Warning: scoped lifetime is not recommended to use in desktop app.
					services.AddScoped(type);
					break;
				case ServiceLifetime.Singleton:
					services.AddSingleton(type);
					break;
				default:
					throw new ArgumentOutOfRangeException(type.FullName, attr.Lifetime, $"Invalid service lifetime for UI element {type.FullName} as {attr.Lifetime} is not supported." );
			}
		}
		
		return services;
	}
}