using ProjectAFS.Core.I18n;

namespace ProjectAFS.Core.Abstractions.Localization;

public interface II18nExtensibleLocalizationLocator
{
	IEnumerable<KeyValuePair<string, LocalizedString>> GetAppendedLocalizedStrings();
}