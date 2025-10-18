using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProjectAFS.Core.Data.Localization;
using Version = SemanticVersioning.Version;

namespace ProjectAFS.Core.Internal.Plugins;

[Serializable, JsonObject]
public class PluginInfo
{
	[JsonProperty("id")]
	public string Id { get; init; } = string.Empty;
	
	[JsonProperty("name")]
	public string Name { get; init; } = string.Empty;
	
	[JsonConverter(typeof(VersionConverter))]
	public Version Version { get; init; } = Version.Parse("0.0.0");
	
	[JsonProperty("author")]
	public string Author { get; init; } = string.Empty;
	
	[JsonProperty("entry_point")]
	public string EntryPointLibrary { get; init; } = string.Empty; // based on Plugin Package Root

	[JsonProperty("description")]
	public LocalizedString Description { get; init; } = LocalizedString.Empty;
	
	[JsonProperty("dependencies")]
	public List<PluginDependency> Dependencies { get; init; } = new();
	
	[JsonIgnore]
	public PluginStatus Status { get; set; } = PluginStatus.Disabled;
	
	[JsonIgnore]
	public string InstallPath { get; set; } = string.Empty;
}