using Microsoft.Extensions.Hosting;
using ProjectAFS.Core.Abstracts.Services.Extensibility;

namespace ProjectAFS.Core.Services.Extensibility;

public sealed class PluginService : IHostedService, IPluginService
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		
	}
	
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		
	}
}