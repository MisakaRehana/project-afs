using System.ComponentModel;
using ProjectAFS.Core.Utility;

namespace ProjectAFS.Core.Models.Globalization;

public enum LanguageType
{
	[Description("")] None = 0,
	[Description("en")] English = 1,
	[Description("ja")] Japanese = 2,
	[Description("ko"), NotImplemented] Korean = 3,
	[Description("zh-Hans")] ChineseSimplified = 4,
	[Description("zh-Hant")] ChineseTraditional = 5
}