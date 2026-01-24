using ProjectAFS.Core.Models.Globalization;

namespace ProjectAFS.Core.Abstracts.Services.Globalization;

public interface II18nCustomProvider
{
	public IReadOnlyDictionary<LanguageType, KeyValuePair<string, IValue>[]> GetAllTranslations();
}