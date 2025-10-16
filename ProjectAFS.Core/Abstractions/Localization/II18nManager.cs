using ProjectAFS.Core.I18n;

namespace ProjectAFS.Core.Abstractions.Localization;

public interface II18nManager
{
	public IValue this[string key] { get; }
	public I18nDict CurrentLanguage { get; }
	
	public void LoadLanguages();
	public void SetLanguage(I18nLanguage language);
	public bool HasKey(string key);
	public IValue GetValue(string key);

	public IEnumerable<I18nDict> GetAllLanguages();
}