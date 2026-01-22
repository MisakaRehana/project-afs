using Newtonsoft.Json;

namespace ProjectAFS.Core.Utility.Json;

public sealed class VersionConverter : JsonConverter<Version>
{
	public override bool CanRead => true;
	public override bool CanWrite => true;
	
	public override Version ReadJson(JsonReader reader, Type objectType, Version? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.String)
		{
			string? versionString = reader.Value!.ToString();
			if (Version.TryParse(versionString, out Version? version))
			{
				return version;
			}
			throw new JsonSerializationException($"Invalid version string: '{versionString}'.");
		}
		throw new JsonSerializationException($"Unexpected token parsing version. Expected String, got {reader.TokenType}.");
	}
	
	public override void WriteJson(JsonWriter writer, Version? value, JsonSerializer serializer)
	{
		if (value == null)
		{
			writer.WriteNull();
			return;
		}
		writer.WriteValue(value.ToString());
	}
}