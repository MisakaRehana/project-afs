using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectAFS.Core.Services.Startup;
using ProjectAFS.Core.Utility.Hosting;
using ProjectAFS.Core.Utility.Threading;
using NMsgBox = ProjectAFS.Core.Utility.Native.NativeMessageBox;

namespace ProjectAFS.Core;

public sealed class AFSApp : Application
{
	private IHost? _host;
	
	/// <summary>
	/// Fetches a service of type <typeparamref name="T"/> from the generic host's service provider.
	/// </summary>
	/// <param name="serviceType">The type of the service to fetch.</param>
	public object this[Type serviceType] => FetchService(serviceType);
	
	public override void Initialize()
	{
		Styles.Add(new FluentTheme());
	}
	
	public override void OnFrameworkInitializationCompleted()
	{
		string[] args = Environment.GetCommandLineArgs() is { Length: > 1 } commandLineArgs
			? commandLineArgs[1..]
			: [];
		string[] hostArgs = args.Where(arg => arg.StartsWith("--H"))
			.Select(arg => arg[3..])
			.ToArray();
		
		SetupApplicationServices();
		StartGenericHost(hostArgs);
		
		base.OnFrameworkInitializationCompleted();
	}
	
	private void SetupApplicationServices()
	{
		switch (ApplicationLifetime)
		{
			case IClassicDesktopStyleApplicationLifetime desktop:
				desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
				break;
			case ISingleViewApplicationLifetime singleView:
				singleView.MainView = null; // This will be set from IHostingService later.
				break;
		}
	}
	
	private void StartGenericHost(string[] hostArgs)
	{
		if (Design.IsDesignMode) return;
		var progressProxy = new StartupProgressProxy();
		
		var builder = Host.CreateDefaultBuilder(hostArgs);
		if (ApplicationLifetime is not null)
		{
			// builder.ConfigureServices((ctx, s) => s.AddSingleton(ApplicationLifetime));
			builder.AddSingleton(ApplicationLifetime); // see ProjectAFS.Core.Utility.Hosting.DependencyInjectionExtension for more information.
		}

		builder.AddSingleton(progressProxy);
		builder.ConfigureServices(Startup.ConfigureServices);
		_host = builder.Build();
		Task.Run(async () =>
		{
			try
			{
				await _host.StartAsync();
			}
			catch (Exception ex)
			{
				await FailFastGenericHost(ex);
			}
		});
	}

	private async AFSTask FailFastGenericHost(Exception ex)
	{
		await AFSTask.SwitchToMainThread();
		await File.AppendAllTextAsync("fatal_error.log", $"[{DateTime.Now}]\n{ex}\n\n");
		if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
		{
			NMsgBox.Show("An unexpected fatal error occurred and caused the application to crash. " +
			             "Please consider any recent changes you made (e.g., installing a new plugin) that might have caused this issue " +
			             "(See https://docs.misakacastle.moe/project-afs/troubleshooting for more information). " +
			             "If this is not a known issue, please file a bug. " +
			             "This is an unrecoverable error and application will be shut down.",
				"project-afs Fatal Error", NMsgBox.MessageBoxButtons.OK, NMsgBox.MessageBoxIcon.Error);
		}
		else
		{
			// For mobile platforms, we just log to console as message boxes may not be available.
			Console.WriteLine("Fatal Error: An unexpected fatal error occurred and caused the application to crash.\n" +
			                  "See fatal_error.log for details.\n" +
			                  "Debug manual can be found at https://docs.misakacastle.moe/project-afs/troubleshooting\n\n" +
			                  "Debug Information:\n" + ex);
		}
		switch (ApplicationLifetime)
		{
			case IClassicDesktopStyleApplicationLifetime desktop:
				desktop.Shutdown(1);
				break;
			case ISingleViewApplicationLifetime:
				// No explicit shutdown method for single view lifetime; just exit process.
				Environment.Exit(1);
				break;
		}
	}
	
	public T FetchService<T>() where T : notnull
	{
		if (_host == null)
		{
			throw new InvalidOperationException("Generic host is not initialized. Please start generic host first.");
		}
		return _host.Services.GetRequiredService<T>();
	}
	
	private object FetchService(Type serviceType)
	{
		if (_host == null)
		{
			throw new InvalidOperationException("Generic host is not initialized. Please start generic host first.");
		}
		return _host.Services.GetRequiredService(serviceType);
	}
}