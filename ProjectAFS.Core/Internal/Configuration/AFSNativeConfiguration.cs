using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Claunia.PropertyList;
using IniParser;
using IniParser.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjectAFS.Core.Utility.Services;
using ProjectAFS.Extensibility.Abstractions.Configuration;

namespace ProjectAFS.Core.Internal.Configuration;

/// <summary>
/// Represents a native configuration storage for Project AFS that utilizes the Windows Registry on Windows,<br />
/// INI files on Linux, and Property List files on macOS for storing application settings.
/// </summary>
[DefaultImplementation(typeof(IAFSConfiguration))]
public class AFSNativeConfiguration : IAFSConfiguration
{
	private const string RegistryBasePath = @"Software\Misaka Castle\project-afs";
	private const string LinuxConfigDir = ".config/project-afs";
	private const string LinuxConfigFile = "profile.conf";
	private const string OSXConfigDir = "Library/Preferences/Misaka Castle/project-afs";
	private const string OSXConfigFile = "profile.plist";
	
	public IReadOnlyDictionary<string, object?> AppSettings => new ReadOnlyDictionary<string, object?>(_settings);
	private readonly ConcurrentDictionary<string, object?> _settings = new();
	private readonly JsonSerializerSettings _jsonSettings = new()
	{
		TypeNameHandling = TypeNameHandling.Auto,
		Formatting = Formatting.Indented,
		NullValueHandling = NullValueHandling.Ignore
	};

	public object this[string key]
	{
		get
		{
			ArgumentNullException.ThrowIfNull(key);
			if (_settings.TryGetValue(key, out var v)) return v!;
			throw new KeyNotFoundException($"Key '{key}' not found in configuration.");
		}
		set
		{
			ArgumentNullException.ThrowIfNull(key);
			_settings[key] = value ?? throw new ArgumentNullException(nameof(value), "Configuration value cannot be null.");
		}
	}

	public bool TryGetValue<T>(string key, out T? value) where T : notnull
	{
		value = default;
		ArgumentNullException.ThrowIfNull(key);
		if (!_settings.TryGetValue(key, out var obj) || obj == null) return false;

		if (obj is T t)
		{
			value = t;
			return true;
		}

		try
		{
			if (obj is JToken jt)
			{
				value = jt.ToObject<T>();
				return value != null;
			}

			value = (T) Convert.ChangeType(obj, typeof(T));
			return true;
		}
		catch
		{
			return false;
		}
	}

	public T GetValue<T>(string key, T defaultValue = default!) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(key);
		if (TryGetValue<T>(key, out var v)) return v!;
		return defaultValue;
	}

	public void SetValue<T>(string key, T value) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(key);
		ArgumentNullException.ThrowIfNull(value);
		_settings[key] = value;
	}

	public async ValueTask LoadAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		_settings.Clear();

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			using var baseKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryBasePath, writable: false)
			                    ?? Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegistryBasePath);
			foreach (var name in baseKey.GetValueNames())
			{
				object? raw = baseKey.GetValue(name);
				if (raw == null) continue;

				if (raw is string s)
				{
					string trimmed = s.TrimStart();
					if (trimmed.StartsWith('{') || trimmed.StartsWith('[') || trimmed.StartsWith('"'))
					{
						try
						{
							var jt = JToken.Parse(s);
							_settings[name] = jt;
							continue;
						}
						catch
						{
							// not JSON; keep as string
						}
					}
					_settings[name] = s;
				}
				else
				{
					// Registry returns numbers or byte[] for certain kinds; keep raw object
					_settings[name] = raw;
				}
			}
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), LinuxConfigDir);
			string file = Path.Combine(path, LinuxConfigFile);
			if (!File.Exists(file))
			{
				Directory.CreateDirectory(path);
				await File.WriteAllTextAsync(file, "; Project AFS Configuration File\n\n[Settings]\n", Encoding.UTF8, cancellationToken);
			}

			var parser = new FileIniDataParser();
			IniData data;
			try
			{
				data = parser.ReadFile(file);
			}
			catch
			{
				return; // ignore malformed file
			}

			var section = data.Sections.ContainsSection("Settings") ? data["Settings"] : data.Global;
			foreach (var key in section)
			{
				string? val = section[key.KeyName];
				if (string.IsNullOrEmpty(val))
				{
					_settings[key.KeyName] = string.Empty;
					continue;
				}

				// try parse as JSON
				try
				{
					var jt = JToken.Parse(val);
					_settings[key.KeyName] = jt;
				}
				catch
				{
					// not JSON -> treat as plain string
					_settings[key.KeyName] = val;
				}
			}
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), OSXConfigDir);
			string file = Path.Combine(path, OSXConfigFile);

			if (!File.Exists(file))
			{
				return;
			}

			try
			{
				var root = (NSDictionary) PropertyListParser.Parse(file);
				foreach (string? key in root.Keys)
				{
					var nsObject = root[key];
					object? clrObject = ConvertFromNSObject(nsObject);
					if (clrObject != null)
					{
						_settings[key] = clrObject;
					}
				}
			}
			catch
			{
				// ignore malformed file
			}
		}
		else
		{
			// fallback JSON file in user profile
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".project-afs");
			string file = Path.Combine(path, "config.json");
			if (!File.Exists(file))
			{
				Directory.CreateDirectory(path);
				await File.WriteAllTextAsync(file, "{}", Encoding.UTF8, cancellationToken);
			}
			string txt = await File.ReadAllTextAsync(file, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
			try
			{
				var jo = JObject.Parse(txt);
				foreach (var prop in jo.Properties())
				{
					_settings[prop.Name] = prop.Value;
				}
			}
			catch
			{
				// ignore
			}
		}
	}

	public async ValueTask SaveAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			using var baseKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryBasePath, writable: true)
			                    ?? Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegistryBasePath);
			foreach (var kv in _settings)
			{
				object? value = kv.Value;
				if (value == null)
				{
					baseKey.SetValue(kv.Key, string.Empty, Microsoft.Win32.RegistryValueKind.String);
					continue;
				}

				switch (value)
				{
					case int i:
						baseKey.SetValue(kv.Key, i, Microsoft.Win32.RegistryValueKind.DWord);
						break;
					case uint ui:
						baseKey.SetValue(kv.Key, unchecked((int)ui), Microsoft.Win32.RegistryValueKind.DWord);
						break;
					case long l:
						baseKey.SetValue(kv.Key, l, Microsoft.Win32.RegistryValueKind.QWord);
						break;
					case ulong ul:
						baseKey.SetValue(kv.Key, unchecked((long)ul), Microsoft.Win32.RegistryValueKind.QWord);
						break;
					case BigInteger bi:
						baseKey.SetValue(kv.Key, bi.ToString(), Microsoft.Win32.RegistryValueKind.String);
						break;
					case bool b:
						baseKey.SetValue(kv.Key, b ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
						break;
					case byte[] bytes:
						baseKey.SetValue(kv.Key, bytes, Microsoft.Win32.RegistryValueKind.Binary);
						break;
					case string s:
						baseKey.SetValue(kv.Key, s, Microsoft.Win32.RegistryValueKind.String);
						break;
					default:
						// complex types: serialize to JSON string
						string json = JsonConvert.SerializeObject(value, _jsonSettings);
						baseKey.SetValue(kv.Key, json, Microsoft.Win32.RegistryValueKind.String);
						break;
				}
			}
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), LinuxConfigDir);
			Directory.CreateDirectory(path);
			string file = Path.Combine(path, LinuxConfigFile);

			var parser = new FileIniDataParser();
			var data = new IniData();
			var section = data.Sections.GetSectionData("Settings") ?? new SectionData("Settings");

			foreach (var kv in _settings.OrderBy(k => k.Key))
			{
				string serialized;
				if (kv.Value == null)
				{
					serialized = string.Empty;
				}
				else if (kv.Value is string s)
				{
					// store plain string as-is
					serialized = s;
				}
				else if (kv.Value is JToken jt)
				{
					serialized = jt.ToString(Formatting.None);
				}
				else
				{
					serialized = JsonConvert.SerializeObject(kv.Value, _jsonSettings);
				}
				
				section.Keys.AddKey(kv.Key, serialized);
			}
			
			data.Sections.Add(section);
			
			string tmp = file + ".tmp";
			parser.WriteFile(tmp, data);
			File.Replace(tmp, file, $"{file}.bak");
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), OSXConfigDir);
			Directory.CreateDirectory(path);
			string file = Path.Combine(path, OSXConfigFile);

			var root = new NSDictionary();
			foreach (var kv in _settings.OrderBy(k => k.Key))
			{
				var nsObject = ConvertToNSObject(kv.Value);
				if (nsObject != null)
				{
					root.Add(kv.Key, nsObject);
				}
			}

			string plistXml = root.ToXmlPropertyList();
			
			string tmp = file + ".tmp";
			await File.WriteAllTextAsync(tmp, plistXml, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
			File.Replace(tmp, file, $"{file}.bak");
		}
		else
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".project-afs");
			string file = Path.Combine(path, "config.json");
			Directory.CreateDirectory(path);
			var dict = _settings.ToDictionary(k => k.Key, v => v.Value);
			string json = JsonConvert.SerializeObject(dict, _jsonSettings);
			string tmp = file + ".tmp";
			await File.WriteAllTextAsync(tmp, json, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
			File.Replace(tmp, file, $"{file}.bak");
		}
	}

	private static object? ConvertFromNSObject(NSObject nsObject)
	{
		switch (nsObject)
		{
			case NSString nsString:
			{
				string s = nsString.ToString(); // same as 'nsString.Content'
				string trimmed = s.TrimStart();
				if (trimmed.StartsWith('{') || trimmed.StartsWith('[') || trimmed.StartsWith('"'))
				{
					try
					{
						var jt = JToken.Parse(s);
						return jt;
					}
					catch
					{
						// not JSON; keep as string
					}
				}
				return s;
			}
			case NSNumber nsNumber:
				return nsNumber.ToObject(); // let Claunia.PropertyList handle number types cause there are many
			case NSData nsData:
				return nsData.Bytes;
			case NSDate nsDate:
				return nsDate.Date;
			default:
				return nsObject.ToObject(); // fallback to universal conversion, may be result in null
		}
	}

	private NSObject ConvertToNSObject(object? value)
	{
		if (value == null) return new NSString(string.Empty);
		switch (value)
		{
			case bool b:
				return new NSNumber(b);
			case int i:
				return new NSNumber(i);
			case long l:
				return new NSNumber(l);
			case float f:
				return new NSNumber(f);
			case double d:
				return new NSNumber(d);
			case byte[] bytes:
				return new NSData(bytes);
			case DateTime dt:
				return new NSDate(dt);
			case DateTimeOffset dto:
				return new NSDate(dto.DateTime);
			case string s:
				return new NSString(s);
			default:
				// complex types: serialize to JSON string and save as NSString
				string json = JsonConvert.SerializeObject(value, _jsonSettings);
				return new NSString(json);
		}
	}
}