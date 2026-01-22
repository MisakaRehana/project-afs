namespace ProjectAFS.Core.Models.Startup;

public record StartupProgressReport(string Message, StartupStage current, bool isAutoLocalized = true);