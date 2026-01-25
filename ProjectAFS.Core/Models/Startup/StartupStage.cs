namespace ProjectAFS.Core.Models.Startup;

public enum StartupStage
{
	HostInitialization = 0,
	CoreServicesInitialization = 1,
	PendingPluginOperations = 2,
	PluginLoading = 3,
	AlmostDone = 4
}