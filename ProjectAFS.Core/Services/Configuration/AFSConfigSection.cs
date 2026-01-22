using System.Collections.Concurrent;
using Newtonsoft.Json;
using ProjectAFS.Core.Abstracts.Services.Configuration;

namespace ProjectAFS.Core.Services.Configuration;

[Serializable]
public sealed class AFSConfigSection : IAFSConfigSection
{
	[JsonProperty("sectionName")]
	public string SectionName { get; }

	[JsonProperty("settings")]
	public ConcurrentDictionary<string, string> Settings { get; }
	
	public string this[string key]
	{
		get => TryGetValue(key);
		set => SetValue(key, value);
	}

	public AFSConfigSection()
	{
		SectionName = string.Empty;
		Settings = new ConcurrentDictionary<string, string>();
	}
	
	[JsonConstructor]
	public AFSConfigSection(string sectionName, ConcurrentDictionary<string, string> settings)
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
	
	public IDictionary<string, string> Diff(IAFSConfigSection other)
	{
		var diffs = new Dictionary<string, string>();
		foreach (var kvp in Settings.Where(kvp => !other.Settings.ContainsKey(kvp.Key) || other.Settings[kvp.Key] != kvp.Value))
		{
			diffs[kvp.Key] = kvp.Value;
		}
		return diffs;
	}
	
	public IAFSConfigSection UpdateTo(IAFSConfigSection baseSection)
	{
		var newSettings = new ConcurrentDictionary<string, string>(baseSection.Settings);
		foreach (var kvp in Settings)
		{
			newSettings[kvp.Key] = kvp.Value;
		}
		return new AFSConfigSection(baseSection.SectionName, newSettings);
	}
}