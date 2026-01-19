using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectAFS.Core.Services;

namespace ProjectAFS.Core;

public static class Startup
{
	public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
	{
		services.AddAFSLogging();
		services.AddAFSConfiguration();
		services.AddI18n();
		services.AddPlugins();
	}
}