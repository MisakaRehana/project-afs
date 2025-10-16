using System.Collections.Concurrent;
using ProjectAFS.Core.Abstractions.Configuration;
using ProjectAFS.Core.Abstractions.Localization;
using ProjectAFS.Core.I18n;

namespace ProjectAFS.Core.Internal.I18n;

public class I18nManager : II18nManager, II18nExtensibleManager
{
	public IValue this[string key] => GetValue(key);
	public I18nDict CurrentLanguage => _languages.TryGetValue(_currentLangId, out var dict) ? dict : _languages["en"];
	private readonly ConcurrentDictionary<I18nLanguage, I18nDict> _languages = [];
	private readonly ConcurrentDictionary<string, LocalizedString> _extLocalizations = [];
	private readonly ConcurrentBag<II18nExtensibleLocalizationLocator> _extLocators = [];
	private string _currentLangId = "en";
	private readonly string _i18nPath;
	
	public I18nManager(IPathOptions pathOptions)
	{
		_i18nPath = pathOptions.I18nLanguagePath;
	}
	
	public I18nManager(IEnumerable<I18nDict> mockLanguages)
	{
		_i18nPath = string.Empty;
		foreach (var dict in mockLanguages)
		{
			_languages[dict.Language] = dict;
		}
	}
	
	public void LoadLanguages()
	{
		_languages.Clear();
		foreach (var file in Directory.GetFiles(_i18nPath, "*.json"))
		{
			var dict = I18nDict.LoadFromFile(file);
			_languages[dict.Language] = dict;
		}
		_extLocalizations.Clear();
		_extLocators.Clear();
	}

	public void SetLanguage(I18nLanguage lang)
	{
		if (_languages.ContainsKey(lang))
		{
			_currentLangId = lang.LangCode;
		}
		else
		{
			throw new ArgumentException($"Language ID '{lang.LangCode}' is not loaded.");
		}
	}
	
	public IEnumerable<I18nDict> GetAllLanguages()
	{
		return _languages.Values.ToArray();
	}

	public bool HasKey(string key)
	{
		return CurrentLanguage.Strings.ContainsKey(key) || _extLocalizations.ContainsKey(key);
	}

	public IValue GetValue(string key)
	{
		return GetValue(key, _currentLangId);
	}
	
	public IValue GetValue(string key, I18nLanguage srcLang)
	{
		if (_extLocalizations.TryGetValue(key, out var extValue) && !string.IsNullOrEmpty(extValue.ToPreferredString(srcLang)))
		{
			return new IValue(extValue.ToPreferredString(srcLang));
		}
		if (_languages.TryGetValue(srcLang, out var dict) && dict.Strings.TryGetValue(key, out var value))
		{
			return value;
		}
		return IValue.Empty(key);
	}

	public void RegisterLocator(II18nExtensibleLocalizationLocator locator)
	{
		_extLocators.Add(locator);
		RefreshExtensibleLocalizations();
	}

	public void UnregisterLocator(II18nExtensibleLocalizationLocator locator)
	{
		var newExtLocators = new List<II18nExtensibleLocalizationLocator>(_extLocators.Where(l => l != locator));
		_extLocators.Clear();
		newExtLocators.ForEach(l => _extLocators.Add(l));
		RefreshExtensibleLocalizations();
	}

	private void RefreshExtensibleLocalizations()
	{
		var allStrings = _extLocators.SelectMany(locator => locator.GetAppendedLocalizedStrings())
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		_extLocalizations.Clear();
		foreach (var kvp in allStrings)
		{
			_extLocalizations[kvp.Key] = kvp.Value;
		}
	}

	public IEnumerable<II18nExtensibleLocalizationLocator> GetRegisteredLocators()
	{
		return _extLocators.ToArray();
	}
}