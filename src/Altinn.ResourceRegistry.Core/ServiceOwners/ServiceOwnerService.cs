#nullable enable

using Altinn.ResourceRegistry.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.ResourceRegistry.Core.ServiceOwners;

/// <summary>
/// Implementation of <see cref="IServiceOwnerService"/>.
/// </summary>
internal partial class ServiceOwnerService
    : IServiceOwnerService
{
    private static readonly TimeSpan StaleAge = TimeSpan.FromHours(1);
    private static readonly TimeSpan ExpiredAge = StaleAge + TimeSpan.FromMinutes(15);

    private readonly ILogger<ServiceOwnerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly object _lock = new();

    private DateTimeOffset _lastUpdated;
    private ServiceOwnerLookup _current = ServiceOwnerLookup.Empty;
    private Task<ServiceOwnerLookup>? _pending;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceOwnerService"/> class.
    /// </summary>
    public ServiceOwnerService(
        ILogger<ServiceOwnerService> logger, 
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public ValueTask<ServiceOwnerLookup> GetServiceOwners(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var lastUpdated = _lastUpdated;
            var duration = _timeProvider.GetUtcNow() - lastUpdated;

            // new enough value, simple case
            if (duration < StaleAge)
            {
                return new(_current);
            }

            // stale value, get pending task or start new
            var task = _pending ??= Task.Run(FetchNew, CancellationToken.None);

            // stale, but not expired - re-fetch in background and return stale value
            if (duration < ExpiredAge)
            {
                return new(_current);
            }

            // expired value, return pending task
            return new(task.WaitAsync(cancellationToken));
        }

        async Task<ServiceOwnerLookup> FetchNew()
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<IOrgListClient>();

            try
            {
                var fetchTime = _timeProvider.GetUtcNow();
                Log.FetchingNewOrgList(_logger);
                var orgList = await client.GetOrgList(CancellationToken.None);
                var duration = _timeProvider.GetUtcNow() - fetchTime;
                Log.FetchedNewOrgList(_logger, duration);

                var serviceOwners = ServiceOwnerLookup.Create(orgList);

                return SetCurrent(fetchTime, serviceOwners);
            }
            catch (Exception ex)
            {
                Log.FailedToFetchNewOrgList(_logger, ex);
                throw;
            }
            finally
            {
                _pending = null;
            }
        }

        ServiceOwnerLookup SetCurrent(DateTimeOffset fetchTime, ServiceOwnerLookup serviceOwners)
        {
            lock (_lock)
            {
                _lastUpdated = fetchTime;
                _current = serviceOwners;
            }

            return serviceOwners;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Fetching new org list")]
        public static partial void FetchingNewOrgList(ILogger logger);

        [LoggerMessage(2, LogLevel.Information, "Fetched new org list in {Duration}")]
        public static partial void FetchedNewOrgList(ILogger logger, TimeSpan duration);

        [LoggerMessage(3, LogLevel.Error, "Failed to fetch new org list")]
        public static partial void FailedToFetchNewOrgList(ILogger logger, Exception exception);
    }
}
