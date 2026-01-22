using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using ProjectAFS.Core.Abstracts.Services.Configuration;

namespace ProjectAFS.Core.Services.Configuration;

public class AFSConfiguration : IAFSConfiguration
{
	private readonly static string ConfigFileName = Path.Combine("$CONFIG", "config.json");
	private readonly static string ConfigBasePath = Path.Combine("$APPDATA", "Misaka Castle", "ProjectAFS");
	private List<IAFSConfigSection> _sections;
	private readonly object _lockObject;

	public AFSConfiguration()
	{
		_sections = [];
		_lockObject = new object();
		LoadConfiguration();
	}

	private void LoadConfiguration()
	{
		lock (_lockObject)
		{
			string configBasePath = ConfigBasePath.Replace("$APPDATA", DetermineAppDataFolderPath());
			string configFilePath = ConfigFileName.Replace("$CONFIG", configBasePath);
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
	}
	
	public IAFSConfigSection GetSection(string sectionName)
	{
		lock (_lockObject)
		{
			if (_sections.Any(s => s.SectionName.Equals(sectionName, StringComparison.OrdinalIgnoreCase)))
			{
				return _sections.First(s => s.SectionName.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
			}
			else
			{
				var newSection = new AFSConfigSection(sectionName, new ConcurrentDictionary<string, string>());
				_sections.Add(newSection);
				Save(newSection);
				return newSection;
			}
		}
	}
	
	public IEnumerable<IAFSConfigSection> GetAllSections()
	{
		lock (_lockObject)
		{
			return _sections.ToList();
		}
	}

	public void Save(IAFSConfigSection section)
	{
		ArgumentNullException.ThrowIfNull(section);
		lock (_lockObject)
		{
			var existingSection = _sections.FirstOrDefault(s => s.SectionName.Equals(section.SectionName, StringComparison.OrdinalIgnoreCase));
			existingSection ??= new AFSConfigSection(section.SectionName, new ConcurrentDictionary<string, string>());
			existingSection = section.UpdateTo(existingSection);
			int index = _sections.FindIndex(s => s.SectionName.Equals(section.SectionName, StringComparison.OrdinalIgnoreCase));
			if (index >= 0)
			{
				_sections[index] = existingSection;
			}
			else
			{
				_sections.Add(existingSection);
			}

			UnsafeSaveAll();
		}
	}

	public void SaveAll()
	{
		lock (_lockObject)
		{
			UnsafeSaveAll();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void UnsafeSaveAll()
	{
		string configBasePath = ConfigBasePath.Replace("$APPDATA", DetermineAppDataFolderPath());
		string configFilePath = ConfigFileName.Replace("$CONFIG", configBasePath);
		string jsonContent = JsonConvert.SerializeObject(_sections, Formatting.Indented);
		File.WriteAllText(configFilePath, jsonContent, new UTF8Encoding());
	}

	private static string DetermineAppDataFolderPath()
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