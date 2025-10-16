namespace ProjectAFS.Core.Abstractions.Localization;

public interface II18nExtensibleManager
{
	void RegisterLocator(II18nExtensibleLocalizationLocator locator);
	void UnregisterLocator(II18nExtensibleLocalizationLocator locator);
	IEnumerable<II18nExtensibleLocalizationLocator> GetRegisteredLocators();
}