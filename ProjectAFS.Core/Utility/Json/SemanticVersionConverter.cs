using Newtonsoft.Json;
using SVersion = SemanticVersioning.Version;

namespace ProjectAFS.Core.Utility.Json;

public sealed class SemanticVersionConverter : JsonConverter<SVersion>
{
	public override bool CanRead => true;
	public override bool CanWrite => true;
	
	public override SVersion ReadJson(JsonReader reader, Type objectType, SVersion? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.String)
		{
			string? versionString = reader.Value!.ToString();
			if (SVersion.TryParse(versionString, out SVersion? version))
			{
				return version;
			}
			throw new JsonSerializationException($"Invalid semantic version string: '{versionString}'.");
		}
		throw new JsonSerializationException($"Unexpected token parsing semantic version. Expected String, got {reader.TokenType}.");
	}
	
	public override void WriteJson(JsonWriter writer, SVersion? value, JsonSerializer serializer)
	{
		if (value == null)
		{
			writer.WriteNull();
			return;
		}
		writer.WriteValue(value.ToString());
	}
}