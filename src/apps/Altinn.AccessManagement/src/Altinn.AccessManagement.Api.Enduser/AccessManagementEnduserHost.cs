using Altinn.AccessManagement.Enduser.Services;
using Altinn.Authorization.Host.Startup;

namespace Altinn.AccessManagement.Api.Enduser;

/// <summary>
/// Configures dependencies for Enduser API.
/// </summary>
public static partial class AccessManagementEnduserHost
{
    private static ILogger Logger { get; } = StartupLoggerFactory.Create(nameof(AccessManagementEnduserHost));

    /// <summary>
    /// Adds access management services for end users to the application builder.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <returns>
    /// The modified <see cref="IHostApplicationBuilder"/> with access management services registered.
    /// </returns>
    public static IHostApplicationBuilder AddAccessManagementEnduser(this IHostApplicationBuilder builder)
    {
        Log.AddHost(Logger);
        builder.Services.AddSingleton<IEnduserConnectionService, ConnectionService>();
        return builder;
    }

    static partial class Log
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "Add access management enduser host.")]
        internal static partial void AddHost(ILogger logger);
    }
}
