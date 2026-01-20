using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace ProjectAFS.Core.Models.Globalization;

[Serializable]
public readonly struct IValue
{
	[JsonProperty("value")] public readonly string Value;

	[JsonProperty("value_c", NullValueHandling = NullValueHandling.Ignore)]
	public readonly string? ValueCute; // see ProjectAFS.Core.Services.AFSChan.IAFSChanManager, ProjectAFS.Plugins.AFSChan and Language JSON files for more information.
	
	[JsonConstructor]
	public IValue(string value)
	{
		Value = value;
	}

	/// <summary>
	/// Returns the localized string representation of the <see cref="IValue"/>.
	/// </summary>
	/// <returns>The localized string.</returns>
	[MethodImpl(MethodImplOptions.NoInlining)] // to support Harmony AOP patching (e.g. see ProjectAFS.Core.Services.AFSChan.AFSChanManager for more information)
	public override string ToString()
	{
		return Value;
	}

	/// <summary>
	/// Creates an <see cref="IValue" /> instance representing a null or missing value for the given key.
	/// </summary>
	/// <param name="key">The key associated with the null or missing value.</param>
	/// <returns>A default <see cref="IValue" /> instance indicating a null or missing value.</returns>
	public static IValue AsNull(string key)
	{
		return new IValue($"<{key}>");
	}
}