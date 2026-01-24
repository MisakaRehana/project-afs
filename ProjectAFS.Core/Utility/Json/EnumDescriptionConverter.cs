using Newtonsoft.Json;
using ProjectAFS.Core.Utility.Enumerable;

namespace ProjectAFS.Core.Utility.Json;

/// <summary>
/// Provides a general JSON converter that serializes and deserializes enum values based on their descriptions.<br />
/// <b>Note:</b> To use this converter with nested types or collections (e.g., List&lt;T&gt;, T[], etc.), use <c>ItemConverter</c> in <see cref="JsonPropertyAttribute"/> instead of <see cref="JsonConverterAttribute"/>.
/// </summary>
/// <seealso cref="EnumDescriptionConverter{T}"/>
public sealed class EnumDescriptionConverter : JsonConverter
{
	public bool Strict { get; }
	
	public EnumDescriptionConverter() : this(false) {}
	
	public EnumDescriptionConverter(bool strict) => Strict = strict;

	public override bool CanConvert(Type objectType)
	{
		var type = Nullable.GetUnderlyingType(objectType) ?? objectType;
		return type.IsEnum;
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		var enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;
		if (reader.TokenType == JsonToken.Null) return null;

		if (reader.TokenType == JsonToken.String)
		{
			string enumString = reader.Value!.ToString()!;

			foreach (object? enumValue in Enum.GetValues(enumType))
			{
				string? description = (enumValue as Enum)?.GetDescription(Strict);
				
				if (string.Equals(description, enumString, StringComparison.OrdinalIgnoreCase))
				{
					return enumValue;
				}
			}
		}

		if (Strict)
		{
			throw new JsonSerializationException($"Unable to convert '{reader.Value}' to enum '{enumType}' in strict mode.");
		}
		
		return Enum.Parse(enumType, reader.Value!.ToString()!);
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (value == null)
		{
			writer.WriteNull();
			return;
		}

		if (value is Enum e)
		{
			writer.WriteValue(e.GetDescription(Strict));
		}
		else
		{
			writer.WriteValue(value.ToString());
		}
	}
}