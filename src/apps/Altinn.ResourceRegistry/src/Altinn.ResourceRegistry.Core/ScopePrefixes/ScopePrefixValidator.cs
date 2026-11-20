#nullable enable

using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Core.ScopePrefixes;

/// <summary>
/// Implementation of <see cref="IScopePrefixValidator"/>.
/// </summary>
/// <remarks>
/// Caches the prefix list the same way <c>ServiceOwnerService</c> caches the org list: a fresh
/// list is used as is, a stale list is used while a new one is fetched in the background, and
/// an expired (or missing) list makes the caller wait for a new one. Only one fetch runs at a time.
/// </remarks>
internal partial class ScopePrefixValidator
    : IScopePrefixValidator
{
    private readonly ILogger<ScopePrefixValidator> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<SamarbeidsportalenSettings> _settings;
    private readonly object _lock = new();

    private DateTimeOffset _lastUpdated;
    private ScopePrefixLookup _current = ScopePrefixLookup.Empty;
    private Task<ScopePrefixLookup>? _pending;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopePrefixValidator"/> class.
    /// </summary>
    public ScopePrefixValidator(
        ILogger<ScopePrefixValidator> logger,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions<SamarbeidsportalenSettings> settings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _settings = settings;
    }

    /// <inheritdoc/>
    public async ValueTask<IReadOnlyList<string>> GetUnownedScopes(string? organizationNumber, IEnumerable<string> scopes, CancellationToken cancellationToken = default)
    {
        var lookup = await GetLookup(cancellationToken);
        var ownedPrefixes = lookup.GetPrefixes(organizationNumber);

        return scopes.Where(scope => !ownedPrefixes.Any(prefix => IsUnderPrefix(scope, prefix))).ToList();
    }

    private static bool IsUnderPrefix(string scope, string prefix)
        => scope.Length > prefix.Length
        && scope[prefix.Length] == ':'
        && scope.StartsWith(prefix, StringComparison.Ordinal);

    private async ValueTask<ScopePrefixLookup> GetLookup(CancellationToken cancellationToken)
    {
        Task<ScopePrefixLookup> task;
        var settings = _settings.Value;

        lock (_lock)
        {
            var age = _timeProvider.GetUtcNow() - _lastUpdated;

            // new enough value, simple case
            if (age < settings.PrefixCacheStaleAge)
            {
                return _current;
            }

            // stale value, get pending task or start new
            task = _pending ??= Task.Run(FetchNew, CancellationToken.None);

            // stale, but not expired - re-fetch in background and return stale value
            if (age < settings.PrefixCacheExpiredAge)
            {
                return _current;
            }
        }

        // expired or never fetched - wait for the pending fetch
        try
        {
            return await task.WaitAsync(cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ScopePrefixDataUnavailableException(ex);
        }
    }

    private async Task<ScopePrefixLookup> FetchNew()
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<ISamarbeidsportalenClient>();

            var fetchTime = _timeProvider.GetUtcNow();
            Log.FetchingPrefixList(_logger);
            var records = await client.GetScopePrefixes(CancellationToken.None);
            var lookup = ScopePrefixLookup.Create(records);

            // An empty list would reject every scope. Treat it as a failure and keep the old list.
            if (lookup.OrganizationCount == 0)
            {
                throw new InvalidOperationException("The prefix list contained no prefixes");
            }

            Log.FetchedPrefixList(_logger, _timeProvider.GetUtcNow() - fetchTime, records.Count, lookup.OrganizationCount);

            lock (_lock)
            {
                _lastUpdated = fetchTime;
                _current = lookup;
                _pending = null;
            }

            return lookup;
        }
        catch (Exception ex)
        {
            Log.FailedToFetchPrefixList(_logger, ex);

            lock (_lock)
            {
                _pending = null;
            }

            throw;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Fetching Maskinporten scope prefix list")]
        public static partial void FetchingPrefixList(ILogger logger);

        [LoggerMessage(2, LogLevel.Information, "Fetched Maskinporten scope prefix list in {Duration}: {RecordCount} records, {OrganizationCount} organizations with prefixes")]
        public static partial void FetchedPrefixList(ILogger logger, TimeSpan duration, int recordCount, int organizationCount);

        [LoggerMessage(3, LogLevel.Error, "Failed to fetch Maskinporten scope prefix list")]
        public static partial void FailedToFetchPrefixList(ILogger logger, Exception exception);
    }
}
