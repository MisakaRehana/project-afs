using System.Collections.Concurrent;

namespace ProjectAFS.Core.Abstracts.Services.Configuration;

public interface IAFSConfigSection
{
	public string SectionName { get; }
	public ConcurrentDictionary<string, string> Settings { get; }
	public string this[string key] { get; set; }
	public string TryGetValue(string key, string @default = "");
	public void SetValue(string key, string value);
	
	public IDictionary<string, string> Diff(IAFSConfigSection other);

	public IAFSConfigSection UpdateTo(IAFSConfigSection baseSection);
}