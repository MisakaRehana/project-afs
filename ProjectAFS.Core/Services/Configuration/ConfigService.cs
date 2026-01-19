using Microsoft.Extensions.Hosting;
using ProjectAFS.Core.Abstracts.Services.Configuration;
using ProjectAFS.Core.Utility.Threading;

namespace ProjectAFS.Core.Services.Configuration;

public sealed class ConfigService : IHostedService, IConfigService
{
	private readonly IAFSConfiguration _configuration;
	
	public ConfigService(IAFSConfiguration configuration) // Dependency Injection
	{
		_configuration = configuration;
	}
	
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await AFSTask.CompletedTask; // config is loaded before host starts
	}
	
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		_configuration.SaveAll();
		await AFSTask.CompletedTask;
	}
}