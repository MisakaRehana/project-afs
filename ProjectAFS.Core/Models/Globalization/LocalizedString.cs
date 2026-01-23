#pragma warning disable AFS0001 // Avoid direct access or conversion of Application.Current to prevent tight coupling with AFSApp. Use Dependency Injection for type AFSApp to enhance testability and maintainability instead.
using Avalonia;
using Newtonsoft.Json;
using ProjectAFS.Core.Abstracts.Services.Globalization;

namespace ProjectAFS.Core.Models.Globalization;

/// <summary>
/// Represents a localized string with multiple language options.
/// </summary>
[Serializable, JsonObject]
public sealed class LocalizedString
{
	[JsonIgnore] public readonly static LocalizedString Empty = new();
	
	[JsonProperty("en")] public string English = string.Empty;
	[JsonProperty("ja", NullValueHandling = NullValueHandling.Ignore)] public string? Japanese;
	[JsonProperty("ko", NullValueHandling = NullValueHandling.Ignore)] public string? Korean;
	[JsonProperty("zh-Hans", NullValueHandling = NullValueHandling.Ignore)] public string? ChineseSimplified;
	[JsonProperty("zh-Hant", NullValueHandling = NullValueHandling.Ignore)] public string? ChineseTraditional;

	/// <summary>
	/// Converts to preferred string based on current language settings.
	/// </summary>
	/// <returns>Preferred localized string.</returns>
	public string ToPreferredString()
	{
		var i18n = (Application.Current as AFSApp)?.FetchService<II18nService>();
		if (i18n == null) return English;
		return i18n.UsingLanguage.LangCode switch
		{
			LanguageType.Japanese => Japanese ?? English,
			LanguageType.Korean => Korean ?? English,
			LanguageType.ChineseSimplified => ChineseSimplified ?? English,
			LanguageType.ChineseTraditional => ChineseTraditional ?? English,
			_ => English
		};
	}
	
	/// <summary>
	/// Converts to preferred string based on current language settings.
	/// </summary>
	/// <returns>Preferred localized string.</returns>
	public override string ToString()
	{
		return ToPreferredString();
	}
}