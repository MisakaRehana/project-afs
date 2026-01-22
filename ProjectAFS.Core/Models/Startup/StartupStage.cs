namespace ProjectAFS.Core.Models.Startup;

public enum StartupStage
{
	HostInitialization = 0,
	PendingPluginOperations = 1,
	PluginLoading = 2,
	AlmostDone = 3
}