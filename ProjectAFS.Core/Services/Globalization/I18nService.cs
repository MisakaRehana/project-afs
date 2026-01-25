using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProjectAFS.Core.Abstracts.Services.Configuration;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.Abstracts.Services.ResourceManagement;
using ProjectAFS.Core.Models.Extensibility;
using ProjectAFS.Core.Models.Globalization;
using ProjectAFS.Core.Utility;
using ProjectAFS.Core.Utility.Enumerable;
using ProjectAFS.Core.Utility.Threading;
using ZLinq;

namespace ProjectAFS.Core.Services.Globalization;

public sealed class I18nService : IHostedService, II18nService
{
	public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
	public ILanguage UsingLanguage { get; private set; } = null!;
	public IValue this[string key] => FetchLocalizedValue(UsingLanguage.LangCode, key);
	public IValue this[LanguageType langType, string key] => FetchLocalizedValue(langType, key);
	private readonly ILogger<I18nService> _logger;
	private readonly IAFSConfiguration _config;
	private readonly IAFSResManager _resManager;
	private readonly Dictionary<LanguageType, ILanguage> _languages;
	private readonly object _customProviderLock = new();

	public I18nService(ILogger<I18nService> logger, IAFSConfiguration config, IAFSResManager resManager) // Dependency Injection
	{
		_resManager = resManager;
		_config = config;
		_logger = logger;
		_languages = new Dictionary<LanguageType, ILanguage>();
	}
		
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await LoadLanguages(cancellationToken);
		UsingLanguage = FetchPreferredLanguage();
	}
		
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		// _config.SaveAll(); // save is handled by AFSConfigurationService
		_languages.Clear();
		await AFSTask.CompletedTask;
	}

	public ILanguage FetchPreferredLanguage()
	{
		string preferredLang = _config.GetSection("Core.Globalization").TryGetValue("Language", DetermineBySystemLanguage());
		var langType = EnumExtension.FindByDescription<LanguageType>(preferredLang);
		if (langType == default)
		{
			langType = LanguageType.English; // fallback to English if not found
		}
		ILanguage? language = null;
		while (language == null)
		{
			if (_languages.TryGetValue(langType, out language))
			{
				return language;
			}

			_logger.LogWarning("Preferred language {Lang} not found. Falling back to another language.", langType);
			langType = FallbackLanguage(langType);
		}
		throw new InvalidOperationException("No any available languages found.");
	}

	public ILanguage FetchPreferredLanguage(LanguageType startLang)
	{
		if (startLang is < LanguageType.English or > LanguageType.ChineseTraditional)
		{
			throw new ArgumentOutOfRangeException(nameof(startLang), "startLang must be a valid LanguageType value.");
		}
		ILanguage? language = null;
		var langType = startLang;
		while (language == null)
		{
			if (_languages.TryGetValue(langType, out language))
			{
				return language;
			}

			_logger.LogWarning("Preferred language {Lang} not found. Falling back to another language.", langType);
			langType = FallbackLanguage(langType);
		}
		throw new InvalidOperationException("No any available languages found.");
	}
		
	public ILanguage FetchLanguage(LanguageType langType, bool strict = true)
	{
		if (_languages.TryGetValue(langType, out var language))
		{
			return language;
		}
		if (strict)
		{
			throw new KeyNotFoundException($"Language {langType} not found.");
		}
		_logger.LogWarning("Requested language {Lang} not found. Falling back to preferred language.", langType);
		return FetchPreferredLanguage(langType);
	}
	
	public ILanguage SwitchLanguage(LanguageType langType)
	{
		var language = FetchLanguage(langType, strict: false);
		var args = new LanguageChangedEventArgs(UsingLanguage, language);
		try
		{
			LanguageChanged?.Invoke(this, args);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while invoking LanguageChanged event.");
		}
		if (args.Cancel)
		{
			_logger.LogInformation("Language switch from {FromLang} to {ToLang} was cancelled by at least one event handler.", UsingLanguage.LangCode, language.LangCode);
			return UsingLanguage;
		}
		else
		{
			_logger.LogDebug("Language switched from {FromLang} to {ToLang}.", UsingLanguage.LangCode, language.LangCode);
			UsingLanguage = language;
			_config.GetSection("Core.Globalization")["Language"] = langType.GetDescription();
			_config.SaveAll();
			return language;
		}
	}

	public void ApplyCustomProvider(AFSPluginInfo plugin, II18nCustomProvider provider)
	{
		var customTrans = provider.GetAllTranslations();
		if (customTrans.Count == 0) return;

		lock (_customProviderLock)
		{
			foreach (var (langType, translations) in customTrans)
			{
				if (!_languages.TryGetValue(langType, out var language))
				{
					_logger.LogWarning("Plugin tried to add translations for unsupported language {Lang}. Skipping.", langType);
					continue;
				}

				foreach ((string key, var newValue) in translations)
				{
					if (language.Texts.ContainsKey(key) && !plugin.IsPermissionGranted(PluginPermission.OverwriteI18n))
					{
						_logger.LogError("Security violation {PermissionId}: Plugin {PluginName} ({PluginId}) tried to overwrite existing translation key '{Key}' in language {LanguageType} " +
						                 "without having the required permission. Skipping this translation.",
							PluginPermission.OverwriteI18n.GetDescription(), plugin.Name, plugin.PluginId, key, langType);
						continue;
					}
					
					language.Texts[key] = newValue;
				}
				_logger.LogDebug("Plugin {PluginName} ({PluginId}) applied {Count} custom translations to language {LanguageType}.",
					plugin.Name, plugin.PluginId, translations.Length, langType);
			}
		}
	}

	private async AFSTask LoadLanguages(CancellationToken cancellationToken = default)
	{
		_languages.Clear();
		await foreach (var language in LoadLanguagesInternalAsync(cancellationToken))
		{
			_languages[language.LangCode] = language;
		}
	}
	
	private async IAsyncEnumerable<ILanguage> LoadLanguagesInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var langReaders = Enum.GetValues<LanguageType>()
			.Where(lang => lang != LanguageType.None)
			.Where(lang => !typeof(LanguageType).GetField(lang.ToString())!
				.GetCustomAttributes<NotImplementedAttribute>().Any())
			.Select(lang => $"Languages/{lang.GetDescription()}.json")
			.Select(langPath => new Lazy<StreamReader?>(() => _resManager.TryOpenTextReader(langPath, out var reader) ? reader as StreamReader : null));
		
		foreach (var lazyReader in langReaders)
		{
			ILanguage? language = null;
			StreamReader? langReader = null;
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				langReader = lazyReader.Value;
				if (langReader == null) continue;
				using (langReader)
				{
					string langJson = await langReader.ReadToEndAsync(cancellationToken);
					language = JsonConvert.DeserializeObject<ILanguage>(langJson);
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Language loading was cancelled.");
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while loading a language.");
				langReader?.Dispose();
			}
			if (language != null)
			{
				yield return language;
			}
			else
			{
				_logger.LogWarning("Failed to deserialize language JSON from {Path}", langReader?.BaseStream);
			}
		}
	}

	private LanguageType FallbackLanguage(LanguageType? prevLang = null)
	{
		prevLang ??= FetchPreferredLanguageType();
		return prevLang switch
		{
			LanguageType.ChineseTraditional => LanguageType.ChineseSimplified,
			LanguageType.ChineseSimplified => LanguageType.English,
			LanguageType.Japanese => LanguageType.English,
			LanguageType.Korean => LanguageType.English,
			LanguageType.English => throw new InvalidOperationException("No available languages to fallback to."),
			_ => LanguageType.English
		};
	}
		
	private LanguageType FetchPreferredLanguageType()
	{
		string preferredLang = _config.GetSection("Core.Globalization").TryGetValue("Language", DetermineBySystemLanguage());
		var langType = EnumExtension.FindByDescription<LanguageType>(preferredLang);
		if (langType == default)
		{
			langType = LanguageType.English; // fallback to English if not found
		}
		return langType;
	}
		
	private IValue FetchLocalizedValue(LanguageType type, string key)
	{
		if (_languages.TryGetValue(type, out var language))
		{
			if (language.Texts.TryGetValue(key, out var value))
			{
				return value;
			}

			_logger.LogWarning("Key {Key} not found in language {Lang}.", key, type);
			return IValue.AsNull(key);
		}

		_logger.LogWarning("Language {Lang} not found.", type);
		throw new KeyNotFoundException($"Language {type} not found.");
	}

	private static string DetermineBySystemLanguage()
	{
		string sysLang = CultureInfo.CurrentUICulture.Name;
		return sysLang switch
		{
			"ja-JP" or "ja" => "ja",
			"ko-KR" or "ko" => "ko",
			"zh-CN" or "zh-SG" or "zh-CHS" or "zh-Hans" or "zh" => "zh-Hans",
			"zh-TW" or "zh-HK" or "zh-MO" or "zh-CHT" or "zh-Hant" => "zh-Hant",
			_ => "en",
		};
	}
}