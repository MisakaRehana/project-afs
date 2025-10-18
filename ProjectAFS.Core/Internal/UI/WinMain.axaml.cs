#pragma warning disable 8618
#pragma warning disable 9264
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstractions.UI;
using ProjectAFS.Core.Internal.ViewModels.UI;
using ProjectAFS.Core.Utility.Native;

namespace ProjectAFS.Core.Internal.Windows;

[DIRequiredUIElement(ServiceLifetime.Singleton)]
public partial class WinMain : DesignableAvaloniaWindow<WinMainViewModel>
{
	private readonly ILogger<WinMain> _logger;
	
	public WinMain()
	{
		InitializeComponent();
		SetupDataContext();
	}

	[ActivatorUtilitiesConstructor]
	public WinMain(ILogger<WinMain> logger)
	{
		_logger = logger;
		InitializeComponent();
		SetupDataContext();
	}

	protected override WinMainViewModel CreateRuntimeViewModel()
	{
		return new WinMainViewModel();
	}

	protected override void OnClosing(WindowClosingEventArgs args)
	{
		_logger.LogInformation("Main window is closing.");

		if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lt)
		{
			_logger.LogWarning("Application lifetime is not IClassicDesktopStyleApplicationLifetime, cannot shutdown application.");
			return;
		}
		lt.Shutdown();
	}

	private bool ShouldCancelClosing()
	{
		// todo: implement unsaved changes check
		// here we simulate one
		IntPtr hWndOwner = IntPtr.Zero;
		try
		{
			var hWndPlatform = TryGetPlatformHandle();
			if (hWndPlatform != null && hWndPlatform.Handle != IntPtr.Zero)
			{
				hWndOwner = hWndPlatform.Handle;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to get window handle for message box owner.");
		}
		
		var result = NativeMessageBox.Show("There are unsaved changes. Are you sure you want to exit?",
			"Unsaved Changes - project-afs", NativeMessageBox.MessageBoxButtons.YesNo, NativeMessageBox.MessageBoxIcon.Warning,
			owner: hWndOwner);
		if (result == NativeMessageBox.MessageBoxResult.No)
		{
			_logger.LogInformation("User canceled closing the main window due to unsaved changes.");
			return true;
		}

		return false;
	}
}