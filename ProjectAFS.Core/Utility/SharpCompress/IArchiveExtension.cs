using SharpCompress.Archives;

namespace ProjectAFS.Core.Utility.SharpCompress;

public static class ArchiveExtension
{
	public static IArchiveEntry GetEntry<T>(this T archive, string entryKey) where T : IArchive
	{
		foreach (var entry in archive.Entries)
		{
			if (entry.Key?.Equals(entryKey, StringComparison.OrdinalIgnoreCase) == true)
			{
				return entry;
			}
		}

		throw new KeyNotFoundException($"Entry with key '{entryKey}' not found in the archive.");
	}
	
	public static IArchiveEntry GetEntry(this IArchive archive, string entryKey)
	{
		foreach (var entry in archive.Entries)
		{
			if (entry.Key?.Equals(entryKey, StringComparison.OrdinalIgnoreCase) == true)
			{
				return entry;
			}
		}

		throw new KeyNotFoundException($"Entry with key '{entryKey}' not found in the archive.");
	}
	
	public static bool TryGetEntry<T>(this T archive, string entryKey, out IArchiveEntry? entry) where T : IArchive
	{
		foreach (var e in archive.Entries)
		{
			if (e.Key?.Equals(entryKey, StringComparison.OrdinalIgnoreCase) == true)
			{
				entry = e;
				return true;
			}
		}

		entry = null;
		return false;
	}
	
	public static bool TryGetEntry(this IArchive archive, string entryKey, out IArchiveEntry? entry)
	{
		foreach (var e in archive.Entries)
		{
			if (e.Key?.Equals(entryKey, StringComparison.OrdinalIgnoreCase) == true)
			{
				entry = e;
				return true;
			}
		}

		entry = null;
		return false;
	}
}