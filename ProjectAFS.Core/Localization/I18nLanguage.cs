using System.ComponentModel;
using Newtonsoft.Json;

namespace ProjectAFS.Core.I18n;

/// <summary>
/// Represents a language supported by the internationalization (i18n) system.
/// </summary>
public readonly struct I18nLanguage : IEquatable<I18nLanguage>
{
	[JsonProperty("lang_code")] public string LangCode { get; init; }
	[JsonProperty("disp_name")] public string DisplayName { get; init; }
	
	/// <summary>
	/// Returns a string that represents the current language.
	/// </summary>
	/// <returns>A string that represents the current language, in "DisplayName (LangCode)" format.</returns>
	public override string ToString()
	{
		return $"{DisplayName} ({LangCode})";
	}

	public static bool operator ==(I18nLanguage left, I18nLanguage right)
	{
		return left.LangCode == right.LangCode;
	}
	
	public static bool operator !=(I18nLanguage left, I18nLanguage right)
	{
		return !(left == right);
	}
	
	public bool Equals(I18nLanguage other)
	{
		return string.Equals(LangCode, other.LangCode, StringComparison.InvariantCulture);
	}

	public override bool Equals(object? obj)
	{
		return obj is I18nLanguage other && Equals(other);
	}

	public override int GetHashCode()
	{
		return StringComparer.InvariantCulture.GetHashCode(LangCode);
	}
	
	/// <summary>
	/// Create a dummy <see cref="I18nLanguage"/> from a language code string.
	/// </summary>
	/// <param name="langCode">The language code string.</param>
	/// <returns>>A new <see cref="I18nLanguage"/> instance with both LangCode and DisplayName set to the provided langCode.</returns>
	public static implicit operator I18nLanguage(string langCode)
	{
		return new I18nLanguage { LangCode = langCode, DisplayName = langCode };
	}
	
	/// <summary>
	/// Converts an <see cref="I18nLanguage"/> to its language code string representation.
	/// </summary>
	/// <param name="lang">The <see cref="I18nLanguage"/> instance to convert.</param>
	/// <returns>>The language code string of the <see cref="I18nLanguage"/>.</returns>
	public static explicit operator string(I18nLanguage lang)
	{
		return lang.LangCode;
	}
}