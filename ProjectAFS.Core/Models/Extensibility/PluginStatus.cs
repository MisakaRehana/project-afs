using System.ComponentModel;
using Newtonsoft.Json;
using ProjectAFS.Core.Utility.Json;

namespace ProjectAFS.Core.Models.Extensibility;

public enum PluginStatus
{
	Disabled = 0,
	Enabled = 1,
	Faulted = 2,
	Loading = 10,
	Unloading = 11
}

[JsonConverter(typeof(EnumDescriptionConverter), true)]
public enum PluginPermission
{
	None = 0,
	[Description("overwrite_i18n")]
	OverwriteI18n = 1,
}