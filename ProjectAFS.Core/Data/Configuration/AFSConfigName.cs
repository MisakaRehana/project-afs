using Avalonia;
using ProjectAFS.Core.Abstractions;
using ProjectAFS.Core.Abstractions.Localization;

namespace ProjectAFS.Core.Data.Configuration;

public readonly struct AFSConfigName
{
	public string Id { get; init; }
	
	public string NameStr => ToPreferredNameString();
	
	/// <summary>
	/// Get the localized preferred name of the configuration.
	/// </summary>
	/// <returns>The localized preferred name if available; otherwise, the Id.</returns>
	public string ToPreferredNameString()
	{
		var host = (Application.Current as IHostingAvaloniaApp)!.Host;
		var i18nMgr = host.Services.GetService(typeof(II18nManager)) as II18nManager;
		return i18nMgr?[$"config.name.{Id}"].ToString() ?? $"config.name.{Id}";
	}
}