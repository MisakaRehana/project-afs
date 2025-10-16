using Microsoft.Extensions.Logging;

namespace ProjectAFS.Core.Abstractions.Logging;

// ReSharper disable once TypeParameterCanBeVariant
public interface ILoggerClient<T> where T : ILoggerClient<T>
{
	ILogger<T> Logger { get; }
}