using Avalonia;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ProjectAFS.Core.Abstractions;
using ProjectAFS.Core.Abstractions.Localization;

namespace ProjectAFS.Core.Data.Localization;

/// <summary>
/// Represents a localization markup extension for Avalonia XAML.
/// </summary>
public class LocalizedExtension : MarkupExtension
{
	/// <summary>
	/// The localization key.
	/// </summary>
	public string? Key { get; set; } = null!;
	
	/// <summary>
	/// The design-time fallback value.
	/// </summary>
	public string? DesignTime { get; set; }

	public LocalizedExtension()
	{
		
	}

	public LocalizedExtension(string key)
	{
		Key = key;
	}

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		try
		{
			if (string.IsNullOrEmpty(Key))
			{
				return string.Empty;
			}

			if (Avalonia.Controls.Design.IsDesignMode)
			{
				return DesignTime ?? $"[{Key}]";
			}

			var manager = (Application.Current as IHostingAvaloniaApp)?.Host.Services.GetService<II18nManager>();
			if (manager != null)
			{
				return manager[Key].ToString();
			}
		
			return $"[{Key}]"; // fallback
		}
		catch (Exception ex)
		{
#if DEBUG
			System.Diagnostics.Debug.WriteLine($"LocalizedExtension error: {ex}");
#else
			(Application.Current as IHostingAvaloniaApp)?.Host.Services.GetService<ILogger<LocalizedExtension>>()?.LogError(ex, "LocalizedExtension error");
#endif
			return $"[{Key}]";
		}
	}
}