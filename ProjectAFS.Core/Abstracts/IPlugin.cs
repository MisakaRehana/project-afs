using Microsoft.Extensions.DependencyInjection;
using Version = SemanticVersioning.Version;

namespace ProjectAFS.Core.Abstracts;

public abstract class AFSBasePlugin : IDisposable
{
	public abstract string Name { get; }
	public abstract Version Version { get; }
	public abstract void ConfigureServices(IServiceCollection services);
	public abstract void Dispose();
}