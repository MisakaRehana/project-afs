namespace ProjectAFS.Extensibility.Abstractions.Configuration;

public interface IAFSConfiguration
{
	IReadOnlyDictionary<string, object?> AppSettings { get; }

	bool TryGetValue<T>(string key, out T? value) where T : notnull;

	T GetValue<T>(string key, T defaultValue = default!) where T : notnull;
	
	void SetValue<T>(string key, T value) where T : notnull;
	
	object this[string key] { get; set; }
	
	ValueTask LoadAsync(CancellationToken cancellationToken = default);
	
	ValueTask SaveAsync(CancellationToken cancellationToken = default);
}