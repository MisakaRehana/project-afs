using System.Collections.ObjectModel;
using System.Reactive;
using ProjectAFS.Core.Abstractions.Localization;
using ProjectAFS.Core.I18n;
using ReactiveUI;

namespace ProjectAFS.Core.Internal.ViewModels.UI;

public class WinLanguageSelectViewModel : ReactiveObject
{
	public ObservableCollection<I18nDict> Languages { get; } = [];
	
	private I18nDict? _selectedLanguage;
	
	public I18nDict? SelectedLanguage
	{
		get => _selectedLanguage;
		set => this.RaiseAndSetIfChanged(ref _selectedLanguage, value);
	}
	
	public ReactiveCommand<Unit, I18nDict?> ConfirmCommand { get; }

	public WinLanguageSelectViewModel()
	{
		ConfirmCommand = ReactiveCommand.Create(() => SelectedLanguage);
	}

	public static WinLanguageSelectViewModel FromI18nManager(II18nManager manager)
	{
		var vm = new WinLanguageSelectViewModel();
		foreach (var lang in manager.GetAllLanguages())
		{
			vm.Languages.Add(lang);
		}
		vm.SelectedLanguage = null; // leave blank to force user selection
		return vm;
	}
}