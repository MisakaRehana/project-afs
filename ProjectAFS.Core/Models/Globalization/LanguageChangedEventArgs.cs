namespace ProjectAFS.Core.Models.Globalization;

public sealed class LanguageChangedEventArgs : EventArgs
{
	public ILanguage OldLanguage { get; }
	public ILanguage NewLanguage { get; }
	public bool Cancel { get; set; }

	public LanguageChangedEventArgs(ILanguage oldLanguage, ILanguage newLanguage)
	{
		OldLanguage = oldLanguage;
		NewLanguage = newLanguage;
		Cancel = false;
	}
}