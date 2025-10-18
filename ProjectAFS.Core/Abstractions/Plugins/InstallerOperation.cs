namespace ProjectAFS.Core.Abstractions.Plugins;

public record struct InstallerOperation(OperationType Type, string PluginId, string SourcePath, string DestinationPath, DateTimeOffset ScheduledAtUtc);