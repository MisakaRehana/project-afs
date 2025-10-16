using System.ComponentModel;
using System.Reflection;
using Newtonsoft.Json;

namespace ProjectAFS.Core.Utility.Json;

public class EnumDescriptionConverter<T> : JsonConverter<T> where T : struct, Enum
{
	public override bool CanRead => true;
	public override bool CanWrite => true;
	
	public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			throw new JsonSerializationException($"Cannot convert null value to enum {typeof(T).Name}");
		}
		
		if (reader.TokenType == JsonToken.String)
		{
			string? enumString = reader.Value?.ToString();
			foreach (var enumValue in Enum.GetValues(typeof(T)).Cast<T>())
			{
				if (string.Equals(GetDescription(enumValue), enumString, StringComparison.InvariantCultureIgnoreCase) ||
				    string.Equals(enumValue.ToString(), enumString, StringComparison.InvariantCultureIgnoreCase))
				{
					return enumValue;
				}
			}
		}
		throw new JsonSerializationException($"Unable to convert value {reader.Value} to enum {typeof(T).Name}");
	}
	
	public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
	{
		string description = GetDescription(value);
		writer.WriteValue(description);
	}
	
	private static string GetDescription<TArg>(TArg value) where TArg : struct, Enum
	{
		var fieldInfo = value.GetType().GetField(value.ToString());
		var descriptionAttribute = fieldInfo?.GetCustomAttribute<DescriptionAttribute>();
		return descriptionAttribute?.Description ?? value.ToString();
	}
}