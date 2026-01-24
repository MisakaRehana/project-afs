using ProjectAFS.Core.Models.Extensibility;
using ProjectAFS.Core.Models.Globalization;

namespace ProjectAFS.Core.Abstracts.Services.Globalization;

public interface II18nService
{
	public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
	public ILanguage UsingLanguage { get; }
	public IValue this[string key] { get; }
	public IValue this[LanguageType langType, string key] { get; }
	public ILanguage FetchPreferredLanguage();
	public ILanguage FetchPreferredLanguage(LanguageType startLang);
	public ILanguage FetchLanguage(LanguageType langType, bool strict = true);
	public ILanguage SwitchLanguage(LanguageType langType);
	
	public void ApplyCustomProvider(AFSPluginInfo plugin, II18nCustomProvider provider);
}