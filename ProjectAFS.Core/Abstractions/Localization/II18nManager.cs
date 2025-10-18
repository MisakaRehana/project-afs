using ProjectAFS.Core.Data.Localization;

namespace ProjectAFS.Core.Abstractions.Localization;

public interface II18nManager
{
	public IValue this[string key] { get; }
	public string this[string key, params object?[] args] { get; }
	
	public I18nDict CurrentLanguage { get; }
	
	public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
	
	public void LoadLanguages();
	public void SetLanguage(I18nLanguage language);
	public bool HasKey(string key);
	public IValue GetValue(string key);

	public IEnumerable<I18nDict> GetAllLanguages();
}