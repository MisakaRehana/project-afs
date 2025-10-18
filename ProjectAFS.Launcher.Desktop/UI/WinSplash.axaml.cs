#pragma warning disable 8618
#pragma warning disable 9264
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstractions.Localization;
using ProjectAFS.Core.Abstractions.Plugins;
using ProjectAFS.Core.Abstractions.UI;
using ProjectAFS.Core.Internal.Windows;
using ProjectAFS.Core.Utility.Threading;
using ProjectAFS.Extensibility.Abstractions.Configuration;

namespace ProjectAFS.Launcher.Desktop.Windows;

[DIRequiredUIElement(ServiceLifetime.Singleton)]
public partial class WinSplash : Window
{
	private readonly IHost _host;
	private readonly ILogger<WinSplash> _logger;
	private readonly IAFSConfiguration _config;
	private readonly II18nManager _i18n;
	private readonly IPluginInstaller _pluginInstaller;
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
	public WinSplash(IHost host, ILogger<WinSplash> logger, IAFSConfiguration config, II18nManager i18n, IPluginInstaller pluginInstaller, IPluginManager pluginManager)
	{
		_host = host;
		_logger = logger;
		_config = config;
		_i18n = i18n;
		_pluginInstaller = pluginInstaller;
		_pluginManager = pluginManager;
		InitializeComponent();
		Grd_FooterLoading.IsVisible = false;
	}
	
	private async Task MainIDELoadAsync()
	{
		try
		{
			const int totalSteps = 4;
			int step = 0;
			Pgbr_Loading.Value = 0; // MinValue = 0f, MaxValue = 1f
		
			#region Generic Host Loading (Step 0)
			await _config.LoadAsync();
			_i18n.LoadLanguages();
			string langCode = _config.GetValue("general.language", string.Empty);
			bool requireInitI18n = false;
			if (string.IsNullOrEmpty(langCode))
			{
				requireInitI18n = true;
				langCode = CultureInfo.CurrentCulture.Name switch
				{
					"zh" or "zh-CN" or "zh-SG" or "zh-Hans" => "zh-Hans",
					"zh-TW" or "zh-HK" or "zh-MO" or "zh-Hant" => "zh-Hant",
					"ja" or "ja-JP" => "ja-JP",
					"ko" or "ko-KR" => "ko-KR",
					_ => "en"
				};
			}
			if (requireInitI18n)
			{
				_i18n.SetLanguage(langCode); // implicit convert from string to I18nLanguage
			}
			bool isAFSChanEnabled = _config.GetValue("general.features.afschan.enabled", true);
			#endregion
			step++;
		
			Grd_FooterLoading.IsVisible = true;
			Tbk_StatusText.Text = _i18n[$"splash.init.host{(isAFSChanEnabled ? ".a" : string.Empty)}", step, totalSteps]; // .a == anime styles
			Pgbr_Loading.Value = (float)step / totalSteps;
		
			// Pending Plugin Installations/Uninstallations (Steps 1-2)
			await ExecutePendingPluginOperationsAsync(step, totalSteps, isAFSChanEnabled);
			step += 2;
		
			// Plugin Loading (Steps 3)
			await LoadPluginsAsync(step, totalSteps, isAFSChanEnabled);
			step++;
		
			// Finalizing (Steps 4)
			Finalize(step, totalSteps, isAFSChanEnabled);
		
			Close();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "MainIDELoadAsync top-level catch");
			throw;
		}
	}
	
	private async Task LoadPluginsAsync(int step, int totalSteps, bool isAFSChanEnabled)
	{
		if (await _pluginManager.HasAvailablePluginsAsync())
		{
			Pgbr_Loading.Value = (float)step / totalSteps;
			float percentRangeToNextStep = 1f / totalSteps;
			int pluginsCount = _pluginManager.DiscoverPlugins().Count();
			float percentPerPlugin = percentRangeToNextStep / pluginsCount;
			int pluginLoadStep = step;
			
			_pluginManager.PluginLoading += async (sender, args) =>
			{
				await AFSDispatcher.SwitchToMainThread();
				Tbk_StatusText.Text = _i18n[$"splash.init.plugin{(isAFSChanEnabled ? ".a" : string.Empty)}.loading", args.PluginName, pluginLoadStep, totalSteps];
				Pgbr_Loading.Value += percentPerPlugin;
			};
			await _pluginManager.LoadPluginsAsync(skipDiscovering: true);
		}
	}
	
	private async Task ExecutePendingPluginOperationsAsync(int step, int totalSteps, bool isAFSChanEnabled)
	{
		float percentRangeToNextStep = 1f / totalSteps; // notice here we have two operations: install & uninstall, every operation is a step
		var uninstOperations = _pluginInstaller.GetPendingInstallationsAsync();
		int uninstCount = 0;
		await foreach (var _ in uninstOperations)
		{
			uninstCount++;
		}
		var instOperations = _pluginInstaller.GetPendingUninstallationsAsync();
		int instCount = 0;
		await foreach (var _ in instOperations)
		{
			instCount++;
		}
	
		if (uninstCount > 0)
		{
			float percentPerUninstall = percentRangeToNextStep / uninstCount;
			int stepForUninstall = step;
			_pluginInstaller.ExecutingOperation += async (sender, op) =>
			{
				AFSDispatcher.InvokeAndWait(() =>
				{
					Tbk_StatusText.Text = _i18n[$"splash.init.plugin.pending.uninstall{(isAFSChanEnabled ? ".a" : string.Empty)}", op.PluginId, stepForUninstall, totalSteps];
					Pgbr_Loading.Value += percentPerUninstall;
				}, DispatcherPriority.Background);
			};
		}
	
		if (instCount > 0)
		{
			float percentPerInstall = percentRangeToNextStep / instCount;
			int stepForInstall = step + 1;
			_pluginInstaller.ExecutingOperation += async (sender, op) =>
			{
				AFSDispatcher.InvokeAndWait(() =>
				{
					Tbk_StatusText.Text = _i18n[$"splash.init.plugin.pending.install{(isAFSChanEnabled ? ".a" : string.Empty)}", op.PluginId, stepForInstall, totalSteps];
					Pgbr_Loading.Value += percentPerInstall;
				}, DispatcherPriority.Background);
			};
		}
	
		await AFSDispatcher.SwitchToThreadPool();
		await _pluginInstaller.ExecutePendingOperationsAsync();
		await AFSDispatcher.SwitchToMainThread();
	}
	
	private void Finalize(int step, int totalSteps, bool isAFSChanEnabled)
	{
		Tbk_StatusText.Text = _i18n[$"splash.init.finalizing{(isAFSChanEnabled ? ".a" : string.Empty)}", step, totalSteps];
		Pgbr_Loading.Value = (float)step / totalSteps;
		
		var main = _host.Services.GetRequiredService<WinMain>();
		main.Show();
	}
}