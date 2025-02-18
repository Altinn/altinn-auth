using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.Authorization.Host.Startup;

/// <summary>
/// Provides a singleton logger factory for application startup logging.
/// This should only be used during startup and not after the <see cref="ServiceCollection"/>
/// has been built.
/// </summary>
[ExcludeFromCodeCoverage]
public class StartupLoggerFactory
{
    /// <summary>
    /// A lazily initialized singleton instance of <see cref="ILoggerFactory"/>.
    /// </summary>
    private static readonly Lazy<ILoggerFactory> _instance = new(
        () =>
        {
            return LoggerFactory.Create(builder =>
            {
                builder.AddConfiguration(StartupConfiguration.Instance.GetSection("Logging"));
                builder.AddConsole();
            });
        },
        LazyThreadSafetyMode.ExecutionAndPublication);

    private StartupLoggerFactory() { }

    /// <summary>
    /// Creates a logger instance for the specified type.
    /// </summary>
    /// <typeparam name="T">The type for which the logger is created.</typeparam>
    /// <returns>An <see cref="ILogger{T}"/> instance.</returns>
    public static ILogger<T> Create<T>() => _instance.Value.CreateLogger<T>();

    /// <summary>
    /// Creates a logger instance with the specified name.
    /// </summary>
    /// <param name="name">The name of the logger.</param>
    /// <returns>An <see cref="ILogger"/> instance.</returns>
    public static ILogger Create(string name) => _instance.Value.CreateLogger(name);
}
