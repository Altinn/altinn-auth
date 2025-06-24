using System.Net;
using Altinn.Authorization.AccessManagement.HostedServices;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.Register;
using Microsoft.FeatureManagement;

namespace Altinn.AccessManagement.HostedServices.Services;

/// <summary>
/// Base
/// </summary>
public class BaseSyncService(IAltinnLease lease, IFeatureManager featureManager, IAltinnRegister register)
{
    /// <summary>
    /// Lease
    /// </summary>
    public IAltinnLease Lease { get; } = lease;

    /// <summary>
    /// Features
    /// </summary>
    protected IFeatureManager FeatureManager { get; } = featureManager;

    /// <summary>
    /// Register
    /// </summary>
    public IAltinnRegister Register { get; } = register;

    /// <summary>
    /// Update lease
    /// </summary>
    protected async Task UpdateLease<T>(LeaseResult<T> ls, Action<T> configureLeaseContent, CancellationToken cancellationToken)
        where T : class, new()
    {
        if (ls.Data is { })
        {
            configureLeaseContent(ls.Data);
            await Lease.Put(ls, ls.Data, cancellationToken);
        }
        else
        {
            var content = new T();
            configureLeaseContent(content);
            await Lease.Put(ls, content, cancellationToken);
        }

        await Lease.RefreshLease(ls, cancellationToken);
    }
}

/// <summary>
/// Logs for Sync Services
/// </summary>
public static partial class Log
{
    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Failed to retrieve updated resources from resource register, got {statusCode}")]
    internal static partial void UpdatedResourceError(ILogger logger, HttpStatusCode statusCode);

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Failed to retrieve service owners from resource register, got {statusCode}")]
    internal static partial void ServiceOwnerError(ILogger logger, HttpStatusCode statusCode);

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Error occured while fetching data from register, got {statusCode}")]
    internal static partial void ResponseError(ILogger logger, HttpStatusCode statusCode);

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Processing party with uuid {partyUuid} from register. RetryCount {count}")]
    internal static partial void Party(ILogger logger, string partyUuid, int count);

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "An error occured while streaming data from register")]
    internal static partial void SyncError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Starting register hosted service")]
    internal static partial void StartRegisterSync(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Quit register hosted service")]
    internal static partial void QuitRegisterSync(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Assignment {action} from '{from}' to '{to}' with role '{role}'")]
    internal static partial void AssignmentSuccess(ILogger logger, string action, string from, string to, string role);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Failed to {action} assingment from '{from}' to '{to}' with role '{role}'")]
    internal static partial void AssignmentFailed(ILogger logger, string action, string from, string to, string role);
}
