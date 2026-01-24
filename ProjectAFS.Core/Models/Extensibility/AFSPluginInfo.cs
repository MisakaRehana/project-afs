using Newtonsoft.Json;
using ProjectAFS.Core.Models.Globalization;
using ProjectAFS.Core.Utility.Json;
using Version = SemanticVersioning.Version;

namespace ProjectAFS.Core.Models.Extensibility;

[Serializable, JsonObject]
public sealed class AFSPluginInfo
{
	[JsonProperty("id")]
	public string PluginId { get; init; } = string.Empty;

	[JsonProperty("name")]
	public LocalizedString Name { get; init; } = new();
	
	[JsonConverter(typeof(SemanticVersionConverter))]
	public Version Version { get; init; } = Version.Parse("0.0.0");
	
	[JsonProperty("author")]
	public string Author { get; init; } = string.Empty;
	
	[JsonProperty("entry_point")]
	public string EntryPointLibrary { get; init; } = string.Empty;
	
	[JsonProperty("description")]
	public LocalizedString Description { get; init; } = new();
	
	[JsonProperty("dependencies")]
	public List<PluginDependency> Dependencies { get; init; } = [];
	
	[JsonProperty("permissions")]
	public List<PluginPermission> Permissions { get; init; } = [];
	
	[JsonIgnore]
	public PluginStatus Status { get; set; } = PluginStatus.Disabled;
	
	[JsonIgnore]
	public string InstallPath { get; set; } = string.Empty;
	
	[JsonIgnore]
	public bool IsBuiltIn { get; set; } = false;
	
	public bool IsPermissionGranted(PluginPermission permission)
	{
		return Permissions.Contains(permission);
	}
}