using Newtonsoft.Json;
using ProjectAFS.Core.Models.Globalization;
using ProjectAFS.Core.Utility.Json;

namespace ProjectAFS.Core.Models.Extensibility;

[Serializable, JsonObject]
public sealed class AFSPluginInfo
{
	[JsonProperty("id")]
	public string PluginId { get; init; } = string.Empty;
	
	[JsonProperty("name")]
	public string Name { get; init; } = string.Empty;
	
	[JsonConverter(typeof(SemanticVersionConverter))]
	public System.Version Version { get; init; } = System.Version.Parse("0.0.0");
	
	[JsonProperty("author")]
	public string Author { get; init; } = string.Empty;
	
	[JsonProperty("entry_point")]
	public string EntryPointLibrary { get; init; } = string.Empty;
	
	[JsonProperty("description")]
	public LocalizedString Description { get; init; } = LocalizedString.Empty;
	
	[JsonProperty("dependencies")]
	public List<PluginDependency> Dependencies { get; init; } = [];
	
	[JsonIgnore]
	public PluginStatus Status { get; set; } = PluginStatus.Disabled;
	
	[JsonIgnore]
	public string InstallPath { get; set; } = string.Empty;
}