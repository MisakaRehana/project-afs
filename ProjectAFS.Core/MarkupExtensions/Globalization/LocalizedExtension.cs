using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstracts.Services.Globalization;
using ProjectAFS.Core.Models.Globalization;

namespace ProjectAFS.Core.MarkupExtensions.Globalization;

public sealed class LocalizedExtension : MarkupExtension
{
	public string Key { get; set; } = string.Empty;
	public string DesignTime { get; set; } = string.Empty;
	public LanguageType? LangType { get; set; }

	private readonly AFSApp _app;
	private readonly ILogger<LocalizedExtension> _logger;
	
	public LocalizedExtension()
	{
		_app = Application.Current as AFSApp ?? throw new InvalidOperationException("I18nExtension requires Application.Current to be of type AFSApp.");
		if (Design.IsDesignMode)
		{
			_logger = LoggerFactory.Create(_ => { }).CreateLogger<LocalizedExtension>();
		}
		else
		{
			_logger = _app.FetchService<ILogger<LocalizedExtension>>() ?? throw new InvalidOperationException("ILogger<I18nExtension> service is not available.");
		}
	}

	public override object ProvideValue( IServiceProvider serviceProvider)
	{
		if (!string.IsNullOrEmpty(DesignTime) && Design.IsDesignMode)
		{
			return DesignTime;
		}
		
		var target = serviceProvider.GetService<IProvideValueTarget>();
		if (target is {TargetObject: AvaloniaObject avaObj, TargetProperty: AvaloniaProperty avaProp})
		{
			if (_app.FetchService<II18nService>() is not { } i18n)
			{
				_logger.LogError("II18nService is not available in I18nExtension.");
				return IValue.AsNull(Key).ToString();
			}

			UpdateValue(avaObj, avaProp, i18n);
			
			i18n.LanguageChanged += (s, e) => Dispatcher.UIThread.Post(() => UpdateValue(avaObj, avaProp, i18n));
			
			return GetLocalizedText(i18n);

		}
		
		_logger.LogError("I18nExtension could not retrieve the target object or property.");
		
		return IValue.AsNull(Key).ToString();
	}
	
	private void UpdateValue(AvaloniaObject avaObj, AvaloniaProperty avaProp, II18nService i18n)
	{
		avaObj.SetValue(avaProp, GetLocalizedText(i18n));
	}

	private string GetLocalizedText(II18nService i18n)
	{
		try
		{
			return LangType.HasValue
				? i18n[LangType.Value, Key].ToString() // specific language
				: i18n[Key].ToString(); // current using language
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving localized text for key '{Key}' with LangType '{LangType}'.", Key, LangType);
			return IValue.AsNull(Key).ToString();
		}
	}
}