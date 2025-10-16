using Newtonsoft.Json;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace ProjectAFS.Core.Internal.Plugins;

[Serializable, JsonObject]
public class PluginDependency
{
	[JsonProperty("id")] public string PluginId { get; init; } = string.Empty;
	[JsonProperty("optional")] public bool IsOptional { get; init; }
	[JsonProperty("version")] public string VersionConstraint { get; init; } = "*";
	[JsonIgnore] public Range VersionRange { get; private set; } = Range.Parse("*"); // based on npm semver range 2.0.0

	public PluginDependency() { }
	
	[JsonConstructor]
	public PluginDependency(string id, string version, bool optional = false)
	{
		PluginId = id;
		VersionConstraint = version;
		IsOptional = optional;
		ParseVersionConstraint();
	}

	private void ParseVersionConstraint()
	{
		if (string.IsNullOrWhiteSpace(VersionConstraint))
		{
			return;
		}

		try
		{
			var range = Range.Parse(VersionConstraint);
			VersionRange = range;
		}
		catch (Exception ex)
		{
			throw new ArgumentException($"Invalid version constraint format: {VersionConstraint}", ex);
		}
	}
	
	public bool IsVersionSatisfied(Version version)
	{
		return VersionRange.IsSatisfied(version);
	}
	
	public bool IsVersionSatisfied(string version) => IsVersionSatisfied(Version.Parse(version));
	public bool IsVersionSatisfied(System.Version version) => IsVersionSatisfied(Version.Parse(version.ToString()));
}