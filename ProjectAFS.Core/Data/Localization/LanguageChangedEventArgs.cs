namespace ProjectAFS.Core.Data.Localization;

public class LanguageChangedEventArgs : EventArgs
{
	public I18nLanguage OldLanguage { get; init; }
	public I18nLanguage NewLanguage { get; init; }
}