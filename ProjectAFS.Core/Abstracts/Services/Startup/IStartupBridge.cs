using ProjectAFS.Core.Abstracts.Services.Shell;

namespace ProjectAFS.Core.Abstracts.Services.Startup;

public interface IStartupBridge
{
	void RegisterShell<T>(T shell) where T : class, IWindowManager;
}