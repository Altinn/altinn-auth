using System.Collections.Concurrent;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessManagement.TestUtils.Mocks;

/// <inheritdoc/>
public class PolicyFactoryMock(ILogger<PolicyRepositoryMock> logger) : IPolicyFactory
{
    private ILogger<PolicyRepositoryMock> Logger { get; } = logger;

    /// <summary>
    /// Dictionary of all policies written through mock repositories, keyed by filepath.
    /// </summary>
    public ConcurrentDictionary<string, byte[]> WrittenPolicies { get; } = new();

    /// <inheritdoc/>
    public IPolicyRepository Create(PolicyAccountType account, string filepath)
    {
        return new PolicyRepositoryMock(filepath, Logger, WrittenPolicies);
    }

    /// <inheritdoc/>
    public IPolicyRepository Create(string filepath)
    {
        return new PolicyRepositoryMock(filepath, Logger, WrittenPolicies);
    }
}
