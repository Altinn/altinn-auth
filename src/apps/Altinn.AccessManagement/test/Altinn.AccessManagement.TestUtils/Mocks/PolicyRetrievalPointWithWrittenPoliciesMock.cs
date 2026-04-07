using System.Collections.Concurrent;
using System.Xml;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessManagement.TestUtils.Mocks;

/// <summary>
/// Extended <see cref="PolicyRetrievalPointMock"/> that also resolves delegation policies
/// from <see cref="PolicyFactoryMock.WrittenPolicies"/> before falling back to the file system.
/// Use this mock in tests that write delegation policies (Add/Update) and then read them back
/// (GetInstanceRights/GetResourceRights) within the same test.
/// </summary>
public class PolicyRetrievalPointWithWrittenPoliciesMock : PolicyRetrievalPointMock
{
    private readonly ConcurrentDictionary<string, byte[]> _writtenPolicies;

    /// <summary>
    /// Constructor that wires up the written policies from the registered <see cref="IPolicyFactory"/>.
    /// </summary>
    public PolicyRetrievalPointWithWrittenPoliciesMock(
        IHttpContextAccessor httpContextAccessor,
        ILogger<PolicyRetrievalPointMock> logger,
        IPolicyFactory policyFactory)
        : base(httpContextAccessor, logger)
    {
        if (policyFactory is PolicyFactoryMock mock)
        {
            _writtenPolicies = mock.WrittenPolicies;
        }
    }

    /// <inheritdoc/>
    public override async Task<XacmlPolicy> GetPolicyVersionAsync(string policyPath, string version, CancellationToken cancellationToken = default)
    {
        if (_writtenPolicies != null && policyPath != null && _writtenPolicies.TryGetValue(policyPath, out byte[] policyBytes))
        {
            using XmlReader reader = XmlReader.Create(new MemoryStream(policyBytes));
            return await Task.FromResult(XacmlParser.ParseXacmlPolicy(reader));
        }

        return await base.GetPolicyVersionAsync(policyPath, version, cancellationToken);
    }
}
