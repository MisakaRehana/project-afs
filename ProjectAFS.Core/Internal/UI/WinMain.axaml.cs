#pragma warning disable 8618
#pragma warning disable 9264
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Dock.Model;
using Dock.Avalonia;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstractions.UI;
using ProjectAFS.Core.Internal.ViewModels.UI;

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
		// TODO
		return new WinMainViewModel();
	}
}