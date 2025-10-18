using ProjectAFS.Core.Data.Localization;

namespace ProjectAFS.Core.Abstractions.Localization;

public interface II18nExtensibleLocalizationLocator
{
	IEnumerable<KeyValuePair<string, LocalizedString>> GetAppendedLocalizedStrings();
}