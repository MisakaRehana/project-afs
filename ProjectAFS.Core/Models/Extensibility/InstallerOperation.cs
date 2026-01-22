namespace ProjectAFS.Core.Models.Extensibility;

public record struct InstallerOperation(OperationType Type, string PluginId, string SourcePath, string DestinationPath, DateTimeOffset ScheduledAtUtc);