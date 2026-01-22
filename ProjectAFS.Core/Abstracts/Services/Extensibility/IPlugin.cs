using Microsoft.Extensions.DependencyInjection;
using ProjectAFS.Core.Models.Extensibility;

namespace ProjectAFS.Core.Abstracts.Services.Extensibility;

public interface IPlugin : IDisposable
{
	public AFSPluginInfo PluginInfo { get; }

	public virtual void OnEnable()
	{
		
	}
	
	public virtual void OnDisable()
	{
		
	}
}