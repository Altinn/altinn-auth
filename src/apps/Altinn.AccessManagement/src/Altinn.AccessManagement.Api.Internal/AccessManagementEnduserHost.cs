using Altinn.AccessManagement.Internal.Services;
using Altinn.Authorization.Host.Startup;

namespace Altinn.AccessManagement.Api.Internal;

/// <summary>
/// Configures dependencies for Internal API.
/// </summary>
public static partial class AccessManagementInternalHost
{
    private static ILogger Logger { get; } = StartupLoggerFactory.Create(nameof(AccessManagementInternalHost));

    /// <summary>
    /// Adds access management services to the application builder.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <returns>
    /// The modified <see cref="IHostApplicationBuilder"/> with access management services registered.
    /// </returns>
    public static IHostApplicationBuilder AddAccessManagementInternal(this IHostApplicationBuilder builder)
    {
        Log.AddHost(Logger);
        builder.Services.AddSingleton<InternalConnectionsService, InternalConnectionsService>();
        return builder;
    }

    static partial class Log
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "Add access management internal host.")]
        internal static partial void AddHost(ILogger logger);
    }
}
