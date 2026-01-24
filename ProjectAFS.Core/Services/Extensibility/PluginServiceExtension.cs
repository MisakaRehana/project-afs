using Avalonia;
using Microsoft.Extensions.Logging;
using ProjectAFS.Core.Abstracts.Projects;
using ProjectAFS.Core.Abstracts.Services.Extensibility;
using ProjectAFS.Core.Abstracts.Services.Globalization;

namespace ProjectAFS.Core.Services.Extensibility;

public static class PluginServiceExtension
{
	extension(PluginContext ctx)
	{
		public void BindProvidersFromPlugin(AFSApp app, II18nService i18n)
		{
			if (!ctx.IsLoaded || ctx.Instance == null)
			{
				return;
			}

			// var pluginType = ctx.Instance.GetType();
			// var providerInterface = typeof(II18nCustomProvider);
			// var providerTypes = pluginType.Assembly.GetTypes()
			// 	.Where(t => providerInterface.IsAssignableFrom(t) && t is {IsAbstract: false, IsClass: true});
			//
			// foreach (var providerType in providerTypes)
			// {
			// 	if (app.CreateInstanceWithInjection(providerType) is II18nCustomProvider provider)
			// 	{
			// 		i18n.ApplyCustomProvider(ctx.Info, provider);
			// 	}
			// }
			var customProviders = ctx.GetExports<II18nCustomProvider>();
			foreach (var provider in customProviders)
			{
				i18n.ApplyCustomProvider(ctx.Info, provider);
			}
		}

		public void BindProjectTemplatesFromPlugin(AFSApp app, IPluginService pluginService, ILogger<IPluginService> logger)
		{
			if (!ctx.IsLoaded || ctx.Instance == null)
			{
				return;
			}

			// var pluginType = ctx.Instance.GetType();
			// var templateProviderInterface = typeof(IProjectTemplateProvider);
			// var templateProviderTypes = pluginType.Assembly.GetTypes()
			// 	.Where(t => templateProviderInterface.IsAssignableFrom(t) && t is {IsAbstract: false, IsClass: true});
			//
			// foreach (var providerType in templateProviderTypes)
			// {
			// 	try
			// 	{
			// 		if (app.CreateInstanceWithInjection(providerType) is IProjectTemplateProvider provider)
			// 		{
			// 			pluginService.RegisterProjectTemplateProvider(provider);
			// 		}
			// 	}
			// 	catch (Exception ex)
			// 	{
			// 		logger.LogError(ex, "Failed to bind project template provider {ProviderType} from plugin {PluginId}", providerType.FullName, ctx.Info.PluginId);
			// 	}
			// }
			var templateProviders = ctx.GetExports<IProjectTemplateProvider>();
			foreach (var provider in templateProviders)
			{
				try
				{
					pluginService.RegisterProjectTemplateProvider(provider);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Failed to bind project template provider {ProviderType} from plugin {PluginId}", provider.GetType().FullName, ctx.Info.PluginId);
				}
			}
		}
		
		public IEnumerable<T> GetExports<T>() where T : notnull
		{
			if (!ctx.IsLoaded || ctx.Instance == null)
			{
				return Array.Empty<T>();
			}

			var pluginType = ctx.Instance.GetType();
			var exportInterface = typeof(T);
			var exportTypes = pluginType.Assembly.GetTypes()
				.Where(t => exportInterface.IsAssignableFrom(t) && t is { IsAbstract: false, IsClass: true });

			var exports = new List<T>();
		
#pragma warning disable AFS0001 // get singleton AFSApp is clearified and DI-standalone here
			var app = Application.Current as AFSApp ?? throw new InvalidOperationException("Failed to get current AFSApp instance.");
#pragma warning restore AFS0001
			foreach (var exportType in exportTypes)
			{
				if (app.CreateInstanceWithInjection(exportType) is T export)
				{
					exports.Add(export);
				}
			}

			return exports;
		}
	}
}

public sealed partial class PluginService
{
	private readonly List<IProjectTemplateProvider> projectTemplateProviders = [];
	
	public IEnumerable<IProjectTemplate> GetProjectTemplatesFromPlugins()
	{
		foreach (var provider in projectTemplateProviders)
		{
			IEnumerable<IProjectTemplate> templates;
			try
			{
				templates = provider.GetAvailableProjectTemplates();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to get project templates from provider {ProviderType}", provider.GetType().FullName);
				continue;
			}
			foreach (var template in templates)
			{
				yield return template;
			}
		}
	}

	public void RegisterProjectTemplateProvider(IProjectTemplateProvider provider)
	{
		projectTemplateProviders.Add(provider);
	}
}