using Newtonsoft.Json;

namespace ProjectAFS.Core.Data.Localization;

[Serializable, JsonObject]
public record struct LocalizedString
{
	public readonly static LocalizedString Empty = new() { English = string.Empty };
	
	[JsonProperty("en")]
	public string English { get; init; }
	
	[JsonProperty("ja", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string? Japanese { get; init; }
	
	[JsonProperty("ko", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string? Korean { get; init; }
	
	[JsonProperty("zh-Hans", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string? ChineseSimplified { get; init; }
	
	[JsonProperty("zh-Hant", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string? ChineseTraditional { get; init; }

	public string ToPreferredString(I18nLanguage lang)
	{
		return lang.LangCode switch
		{
			"zh-Hant" => ChineseTraditional ?? string.Empty,
			"zh-Hans" => ChineseSimplified ?? string.Empty,
			"ko" => Korean ?? string.Empty,
			"ja" => Japanese ?? string.Empty,
			_ => English
		};
	}
}