#pragma warning	disable 8618
#pragma warning disable 9264
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ProjectAFS.Core.Abstractions.Localization;
using ProjectAFS.Core.Abstractions.UI;
using ProjectAFS.Core.I18n;
using ProjectAFS.Core.Internal.I18n;
using ProjectAFS.Core.Internal.ViewModels.UI;

namespace ProjectAFS.Core.Internal.Windows;

[DIRequiredUIElement(ServiceLifetime.Singleton)]
public partial class WinLanguageSelect : DesignableAvaloniaWindow<WinLanguageSelectViewModel>
{
	private readonly II18nManager _i18nManager;

	public WinLanguageSelect()
	{
		InitializeComponent();
		SetupDataContext();
	}
	
	[ActivatorUtilitiesConstructor]
	public WinLanguageSelect(II18nManager i18nManager)
	{
		_i18nManager = i18nManager;
		InitializeComponent();
		InitializeLanguageSelect();
	}
	
	private void InitializeLanguageSelect()
	{
		if (DataContext is WinLanguageSelectViewModel vm)
		{
			vm.ConfirmCommand.Subscribe(selected =>
			{
				if (selected != null)
				{
					_i18nManager.SetLanguage(selected.Language.LangCode);
				}
			});
		}
	}

	protected override WinLanguageSelectViewModel CreateDesignTimeViewModel()
	{
		var langs = new List<I18nDict>()
		{
			new() {Language = new I18nLanguage() {LangCode = "en", DisplayName = "English"}},
			new() {Language = new I18nLanguage() {LangCode = "ja", DisplayName = "日本語"}},
			new() {Language = new I18nLanguage() {LangCode = "ko", DisplayName = "한국어"}},
			new() {Language = new I18nLanguage() {LangCode = "zh-Hans", DisplayName = "简体中文"}},
			new() {Language = new I18nLanguage() {LangCode = "zh-Hant", DisplayName = "繁體中文"}}
		};
		var mock = new I18nManager(langs);
		return WinLanguageSelectViewModel.FromI18nManager(mock);
	}

	protected override WinLanguageSelectViewModel CreateRuntimeViewModel()
	{
		return WinLanguageSelectViewModel.FromI18nManager(_i18nManager);
	}
}