using System.ComponentModel;

namespace ProjectAFS.Core.I18n;

public enum I18nTextStyle
{
	[Description("regular")]
	Regular,
	[Description("bold")]
	Bold,
	[Description("italic")]
	Italic,
	[Description("underline")]
	Underline,
	[Description("strikethrough")]
	Strikethrough
}