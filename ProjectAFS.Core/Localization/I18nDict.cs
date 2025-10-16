using System.Text;
using Newtonsoft.Json;

namespace ProjectAFS.Core.I18n;

[JsonDictionary]
public class I18nDict
{
	[JsonProperty("language")] public I18nLanguage Language { get; init; }

	[JsonProperty("strings")] public Dictionary<string, IValue> Strings { get; init; } = [];
	
	public static I18nDict LoadFromFile(string filePath)
	{
		var json = File.ReadAllText(filePath, Encoding.UTF8);
		return JsonConvert.DeserializeObject<I18nDict>(json) ?? throw new InvalidOperationException($"Failed to deserialize I18nDict from file: {filePath}");
	}
}