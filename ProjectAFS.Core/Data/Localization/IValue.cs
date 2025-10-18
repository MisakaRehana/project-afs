using Newtonsoft.Json;
using ProjectAFS.Core.Utility.Json;
using SmartFormat;

namespace ProjectAFS.Core.Data.Localization;

[JsonObject]
public readonly record struct IValue(
	[property: JsonProperty("value")]
	string Value = "",
    
	[property: JsonProperty("style", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
	[property: JsonConverter(typeof(NullableEnumDescriptionConverter<I18nTextStyle>))]
	I18nTextStyle? Style = null
)
{
	/// <summary>
	/// Converts the translation to its raw string value representation.
	/// </summary>
	/// <returns>The raw string value of the translation.</returns>
	public override string ToString() => Value;
	
	/// <summary>
	/// Implicitly converts an <see cref="IValue"/> to a string by returning its Value property.
	/// </summary>
	/// <param name="value">The <see cref="IValue"/> instance to convert.</param>
	/// <returns>The raw string value of the <see cref="IValue"/>.</returns>
	public static implicit operator string(IValue value) => value.Value;
	
	/// <summary>
	/// Converts a string to an <see cref="IValue"/> by creating a new instance with the string as its Value property.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>>A new <see cref="IValue"/> instance with the specified string as its Value property.</returns>
	public static explicit operator IValue(string value) => new IValue(value);
	
	public static IValue Empty(string id) => new($"[{id}]");
	
	/// <summary>
	/// Formats the value string. Supports both positional and named arguments.
	/// </summary>
	/// <param name="args">Positional or named arguments (anonymous object).</param>
	/// <returns>The formatted <see cref="IValue"/> string.</returns>
	public string Format(params object?[] args)
	{
		if (args is [not string and not IEnumerable<object>])
		{
			return Smart.Format(Value, args[0]!);
		}
		else
		{
			return Smart.Format(Value, args);
		}
	}
}