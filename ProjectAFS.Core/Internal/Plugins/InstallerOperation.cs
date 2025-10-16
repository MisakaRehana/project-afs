namespace ProjectAFS.Core.Internal.Plugins;

public record struct InstallerOperation(OperationType Type, string PluginId, string SourcePath, string DestinationPath, DateTimeOffset ScheduledAtUtc);