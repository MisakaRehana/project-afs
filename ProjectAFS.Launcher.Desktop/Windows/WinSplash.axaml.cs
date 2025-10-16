#pragma warning disable 8618
#pragma warning disable 9264
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstractions.Plugins;
using ProjectAFS.Core.Abstractions.UI;

namespace ProjectAFS.Launcher.Desktop.Windows;

[DIRequiredUIElement(ServiceLifetime.Singleton)]
public partial class WinSplash : Window
{
	private readonly IHost _host;
	private readonly ILogger<WinSplash> _logger;
	private readonly IPluginManager _pluginManager;
	
	public WinSplash() // only for design time!
	{
		if (!Design.IsDesignMode)
		{
			throw new InvalidOperationException("Use parameterized constructor instead.");
		}
		InitializeComponent();
	}

	[ActivatorUtilitiesConstructor]
	public WinSplash(IHost host, ILogger<WinSplash> logger, IPluginManager pluginManager)
	{
		_host = host;
		_logger = logger;
		_pluginManager = pluginManager;
		InitializeComponent();
	}
}