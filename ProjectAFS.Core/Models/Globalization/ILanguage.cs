using Newtonsoft.Json;
using ProjectAFS.Core.Utility.Json;

namespace ProjectAFS.Core.Models.Globalization;

[Serializable]
public sealed class ILanguage
{
	[JsonProperty("langCode"), JsonConverter(typeof(EnumDescriptionConverter<LanguageType>))] public LanguageType LangCode;
	[JsonProperty("langName")] public string LangName = string.Empty; // display as its original language, e.g. "日本語", "English", "简体中文"
	[JsonProperty("texts")] public Dictionary<string, IValue> Texts = new(); // key-value pairs of localized texts
}