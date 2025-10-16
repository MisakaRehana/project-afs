using Newtonsoft.Json;

namespace ProjectAFS.Core.Utility.Collections;

/// <summary>
/// Represents a JSON converter for <see cref="ConcurrentHashSet{T}"/> to enable serialization and deserialization using Newtonsoft.Json.
/// </summary>
/// <typeparam name="T">The type of elements in the set. Must be non-nullable.</typeparam>
public class ConcurrentHashSetConverter<T> : JsonConverter<ConcurrentHashSet<T>> where T : notnull
{
	public override bool CanRead => true;
	public override bool CanWrite => true;

	public override ConcurrentHashSet<T>? ReadJson(JsonReader reader, Type objectType, ConcurrentHashSet<T>? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		var items = serializer.Deserialize<List<T>>(reader);
		return new ConcurrentHashSet<T>(items ?? Enumerable.Empty<T>());
	}

	public override void WriteJson(JsonWriter writer, ConcurrentHashSet<T>? value, JsonSerializer serializer)
	{
		var items = value?.ToArray() ?? [];
		serializer.Serialize(writer, items);
	}
}