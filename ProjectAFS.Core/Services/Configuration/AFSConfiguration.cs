using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using ProjectAFS.Core.Abstracts.Services.Configuration;

namespace ProjectAFS.Core.Services.Configuration;

public class AFSConfiguration : IAFSConfiguration
{
	private const string ConfigFileName = "$HOME/Misaka Castle/ProjectAFS/config.json";
	private List<IAFSConfigSection> _sections;

	public AFSConfiguration()
	{
		_sections = [];
		LoadConfiguration();
	}

	private void LoadConfiguration()
	{
		string configBasePath = DetermineConfigFolderPath();
		string configFilePath = ConfigFileName.Replace("$HOME", configBasePath);
		if (!Directory.Exists(configBasePath))
		{
			Directory.CreateDirectory(configBasePath);
		}
		if (File.Exists(configFilePath))
		{
			string jsonContent = File.ReadAllText(configFilePath, new UTF8Encoding());
			var sections = JsonConvert.DeserializeObject<List<AFSConfigSection>>(jsonContent) ?? [];
			_sections = sections.Cast<IAFSConfigSection>().ToList();
		}
		else
		{
			_sections = [];
			SaveAll(); // create an empty config file
		}
	}
	
	public IAFSConfigSection GetSection(string sectionName)
	{
		if (_sections.Any(s => s.SectionName.Equals(sectionName, StringComparison.OrdinalIgnoreCase)))
		{
			return _sections.First(s => s.SectionName.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
		}
		else
		{
			var newSection = new AFSConfigSection(sectionName, new Dictionary<string, string>());
			_sections.Add(newSection);
			return newSection;
		}
	}
	
	public IEnumerable<IAFSConfigSection> GetAllSections()
	{
		foreach (var section in _sections)
		{
			yield return section;
		}
	}

	public void SaveAll()
	{
		string configBasePath = DetermineConfigFolderPath();
		string configFilePath = ConfigFileName.Replace("$HOME", configBasePath);
		string jsonContent = JsonConvert.SerializeObject(_sections, Formatting.Indented);
		File.WriteAllText(configFilePath, jsonContent, new UTF8Encoding());
	}

	private static string DetermineConfigFolderPath()
	{
		if (OperatingSystem.IsWindows())
		{
			return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		}

		if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
		}
		if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
		{
			return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		}

		throw new PlatformNotSupportedException("Unsupported operating system.");
	}
}