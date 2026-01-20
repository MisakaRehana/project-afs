namespace ProjectAFS.Core.Models.Startup;

public record StartupProgressReport(string Message, int currStepNum, int totalSteps, bool isLocalized = false);