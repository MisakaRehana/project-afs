using Newtonsoft.Json;
using ProjectAFS.Core.Utility.Enumerable;

namespace ProjectAFS.Core.Utility.Json;

public sealed class EnumDescriptionConverter<T> : JsonConverter<T> where T : Enum
{
	public override bool CanRead => true;
	public override bool CanWrite => true;
	public bool Strict { get; }
	
	public EnumDescriptionConverter()
	{
		Strict = false;
	}
	
	public EnumDescriptionConverter(bool strict)
	{
		Strict = strict;
	}

	public override T ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		try
		{
			if (reader.TokenType == JsonToken.String)
			{
				string? enumString = reader.Value!.ToString();
				foreach (T enumValue in Enum.GetValues(typeof(T)))
				{
					if (enumValue.GetDescription(Strict).Equals(enumString, StringComparison.OrdinalIgnoreCase))
					{
						return enumValue;
					}
				}
			}
		}
		catch (InvalidOperationException ex)
		{
			throw new JsonSerializationException($"Unable to convert '{reader.Value}' to enum '{typeof(T)}' in strict mode.", ex);
		}
		throw new JsonSerializationException($"Unable to convert '{reader.Value}' to enum '{typeof(T)}'.");
	}

	public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
	{
		try
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			string description = value.GetDescription(Strict);
			writer.WriteValue(description);
		}
		catch (InvalidOperationException ex)
		{
			throw new JsonSerializationException($"Unable to convert enum '{typeof(T)}' to string in strict mode.", ex);
		}
	}
}