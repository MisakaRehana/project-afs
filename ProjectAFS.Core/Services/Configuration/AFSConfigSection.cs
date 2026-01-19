using Newtonsoft.Json;
using ProjectAFS.Core.Abstracts.Services.Configuration;

namespace ProjectAFS.Core.Services.Configuration;

[Serializable]
public sealed class AFSConfigSection : IAFSConfigSection
{
	[JsonProperty("sectionName")]
	public string SectionName { get; }

	[JsonProperty("settings")]
	public Dictionary<string, string> Settings { get; }
	
	public string this[string key]
	{
		get => TryGetValue(key);
		set => SetValue(key, value);
	}

	public AFSConfigSection()
	{
		SectionName = string.Empty;
		Settings = new Dictionary<string, string>();
	}
	
	[JsonConstructor]
	public AFSConfigSection(string sectionName, Dictionary<string, string> settings)
	{
		SectionName = sectionName;
		Settings = settings;
	}
	
	public string TryGetValue(string key, string @default = "")
	{
		return Settings.GetValueOrDefault(key, @default);
	}
	
	public void SetValue(string key, string value)
	{
		Settings[key] = value;
	}
}