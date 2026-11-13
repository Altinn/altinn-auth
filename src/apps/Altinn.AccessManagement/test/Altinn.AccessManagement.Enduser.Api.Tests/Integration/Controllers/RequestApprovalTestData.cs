using System.Xml;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core.Utils.Helper;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Shared setup for tests that approve resource requests for the app <see cref="TestData.SiriusSkattemelding"/>
/// on behalf of <see cref="TestData.DumboAdventures"/>. Its policy gives rights to roles, including managing director
/// (delegable by <see cref="TestData.MalinEmilie"/>), and some rights only to the service owner org (exist, but not delegable).
/// The tests register <see cref="PolicyRetrievalPointMock"/> and <see cref="PolicyFactoryMock"/>, and give each request its
/// own requester, so the delegation policy written for a requester holds only what one test gave.
/// </summary>
internal static class RequestApprovalTestData
{
    public static string AppResource => TestData.SiriusSkattemelding.RefId;

    /// <summary>
    /// The keys of the app policy: those given to roles, sorted, and those given only to the service owner org.
    /// </summary>
    public static (List<string> RoleKeys, List<string> OrgOnlyKeys) GetAppPolicyKeys()
    {
        XacmlPolicy policy = new PolicyRetrievalPointMock().GetPolicyAsync("skd", "sirius-skattemelding-v1").GetAwaiter().GetResult();
        var roleKeys = policy.Rules.Take(4).SelectMany(rule => DelegationCheckHelper.CalculateRightKeys(rule, AppResource)).Distinct().Order(StringComparer.Ordinal).ToList();
        var orgOnlyKeys = policy.Rules.Skip(4).SelectMany(rule => DelegationCheckHelper.CalculateRightKeys(rule, AppResource)).Except(roleKeys).Distinct().ToList();
        return (roleKeys, orgOnlyKeys);
    }

    /// <summary>
    /// Adds a request from <paramref name="requesterId"/> to <see cref="TestData.DumboAdventures"/> for the app.
    /// </summary>
    public static void AddResourceRequest(AppDbContext db, Guid id, Guid requesterId, List<string>? requestedRightKeys, RequestStatusV2 status = RequestStatusV2.Pending)
    {
        var assignment = new RequestAssignment
        {
            FromId = requesterId,
            ToId = TestData.DumboAdventures.Id,
            ById = requesterId,
            RoleId = RoleConstants.Rightholder,
        };
        db.RequestAssignments.Add(assignment);
        db.SaveChanges();

        db.RequestAssignmentResources.Add(new RequestAssignmentResource
        {
            Id = id,
            AssignmentId = assignment.Id,
            ResourceId = TestData.SiriusSkattemelding.Id,
            Status = status,
            RequestedRightKeys = requestedRightKeys,
        });
        db.SaveChanges();
    }

    /// <summary>
    /// The right keys in the delegation policy written for the requester, that is, what was delegated to it.
    /// </summary>
    public static HashSet<string> GetDelegatedRightKeys(ApiFixture fixture, ConstantDefinition<Entity> requester)
    {
        var (_, content) = Assert.Single(GetWrittenPolicies(fixture, requester));

        XacmlPolicy policy;
        using (var reader = XmlReader.Create(new MemoryStream(content)))
        {
            policy = XacmlParser.ParseXacmlPolicy(reader);
        }

        return [.. policy.Rules.SelectMany(rule => DelegationCheckHelper.CalculateRightKeys(rule, AppResource))];
    }

    /// <summary>
    /// Whether a delegation policy was written for the requester, that is, whether anything was delegated to it.
    /// </summary>
    public static bool HasDelegationPolicy(ApiFixture fixture, ConstantDefinition<Entity> requester)
        => GetWrittenPolicies(fixture, requester).Count > 0;

    private static List<KeyValuePair<string, byte[]>> GetWrittenPolicies(ApiFixture fixture, ConstantDefinition<Entity> requester)
    {
        var policyFactory = Assert.IsType<PolicyFactoryMock>(fixture.Server.Services.GetRequiredService<IPolicyFactory>());
        return policyFactory.WrittenPolicies
            .Where(p => p.Key.Contains($"/p{requester.Entity.PartyId}/", StringComparison.Ordinal)
                || p.Key.Contains(requester.Id.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
