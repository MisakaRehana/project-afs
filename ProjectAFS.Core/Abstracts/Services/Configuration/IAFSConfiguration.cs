namespace ProjectAFS.Core.Abstracts.Services.Configuration;

public interface IAFSConfiguration
{
	public IAFSConfigSection GetSection(string sectionName);
	public IEnumerable<IAFSConfigSection> GetAllSections();
	public void Save(IAFSConfigSection section);
	public void SaveAll();
}