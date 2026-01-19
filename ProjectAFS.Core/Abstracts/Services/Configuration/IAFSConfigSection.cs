namespace ProjectAFS.Core.Abstracts.Services.Configuration;

public interface IAFSConfigSection
{
	public string SectionName { get; }
	public Dictionary<string, string> Settings { get; }
	public string this[string key] { get; set; }
	public string TryGetValue(string key, string @default = "");
	public void SetValue(string key, string value);
}