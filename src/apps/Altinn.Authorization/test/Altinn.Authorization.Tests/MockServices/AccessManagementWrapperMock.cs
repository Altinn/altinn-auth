using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Authorization.Enums;
using Altinn.Platform.Authenticaiton.Extensions;
using Altinn.Platform.Authorization.Constants;
using Altinn.Platform.Authorization.IntegrationTests.Data;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Models.AccessManagement;
using Altinn.Platform.Authorization.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic;

namespace Altinn.Platform.Authorization.IntegrationTests.MockServices;

public class AccessManagementWrapperMock : IAccessManagementWrapper
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Constructor setting up dependencies
    /// </summary>
    /// <param name="httpContextAccessor">httpContextAccessor</param>
    public AccessManagementWrapperMock(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<IEnumerable<DelegationChangeExternal>> GetAllDelegationChanges(DelegationChangeInput input, CancellationToken cancellationToken = default)
    {
        var data = new List<Action<DelegationChangeInput, List<DelegationChangeExternal>>>()
        {
            ConditionalAdd(
                DelegationChangesTestData.Default(DelegationChangesTestData.WithResourceID("org1/app1"), DelegationChangesTestData.WithOfferedByPartyID(50001337), DelegationChangesTestData.WithCoveredByUserID(20001337)),
                WithDefaultCondition("org1/app1", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50001335" }, new AttributeMatch { Id = XacmlRequestAttribute.UserAttribute, Value = "20001337" }),
                WithDefaultCondition("org1/app1", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50001337" }, new AttributeMatch { Id = XacmlRequestAttribute.UserAttribute, Value = "20001337" })), // UserDelegation MainUnit Permit
            ConditionalAdd(
                DelegationChangesTestData.Default(DelegationChangesTestData.WithResourceID("org1/app2"), DelegationChangesTestData.WithOfferedByPartyID(50001337), DelegationChangesTestData.WithCoveredByUserID(20001337)),
                WithDefaultCondition("org1/app2", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50001337" }, new AttributeMatch { Id = XacmlRequestAttribute.UserAttribute, Value = "20001337" })),
            ConditionalAdd(
                DelegationChangesTestData.Default(DelegationChangesTestData.WithResourceID("skd/taxreport"), DelegationChangesTestData.WithOfferedByPartyID(1000), DelegationChangesTestData.WithCoveredByUserID(20001337)),
                WithDefaultCondition("skd/taxreport", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "1000" }, new AttributeMatch { Id = XacmlRequestAttribute.UserAttribute, Value = "20001337" })),
            ConditionalAdd(
                DelegationChangesTestData.Default(DelegationChangesTestData.WithResourceID("org1/app1"), DelegationChangesTestData.WithOfferedByPartyID(50001337), DelegationChangesTestData.WithCoveredByPartyID(50001336)),
                WithDefaultCondition("org1/app1", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50001337" }, new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50001336" }),
                WithDefaultCondition("org1/app1", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50001337" }, new AttributeMatch { Id = XacmlRequestAttribute.UserAttribute, Value = "20001336" }),
                WithDefaultCondition("org1/app1", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50001335" }, new AttributeMatch { Id = XacmlRequestAttribute.UserAttribute, Value = "20001336" })),
            ConditionalAdd(
                DelegationChangesTestData.Default(DelegationChangesTestData.WithResourceID("skd/taxreport"), DelegationChangesTestData.WithOfferedByPartyID(50005545), DelegationChangesTestData.WithToUuid(UuidType.SystemUser, Guid.Parse("47caea5b-a80b-4343-b1d3-31eb523a4e28"))),
                WithDefaultCondition("skd/taxreport", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50005545" }, new AttributeMatch { Id = XacmlRequestAttribute.SystemUserIdAttribute, Value = "47caea5b-a80b-4343-b1d3-31eb523a4e28" })),
            ConditionalAdd(
                DelegationChangesTestData.Default(DelegationChangesTestData.WithResourceID("org1/app1"), DelegationChangesTestData.WithOfferedByPartyID(50005545), DelegationChangesTestData.WithToUuid(UuidType.SystemUser, Guid.Parse("47caea5b-a80b-4343-b1d3-31eb523a4e28"))),
                WithDefaultCondition("org1/app1", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50005545" }, new AttributeMatch { Id = XacmlRequestAttribute.SystemUserIdAttribute, Value = "47caea5b-a80b-4343-b1d3-31eb523a4e28" })),
            ConditionalAdd(
                DelegationChangesTestData.Default(DelegationChangesTestData.WithResourceID("ttd-externalpdp-resource1"), DelegationChangesTestData.WithOfferedByPartyID(50005545), DelegationChangesTestData.WithToUuid(UuidType.SystemUser, Guid.Parse("47caea5b-a80b-4343-b1d3-31eb523a4e28"))),
                WithDefaultCondition("ttd-externalpdp-resource1", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50005545" }, new AttributeMatch { Id = XacmlRequestAttribute.SystemUserIdAttribute, Value = "47caea5b-a80b-4343-b1d3-31eb523a4e28" })),
            ConditionalAdd(
                DelegationChangesTestData.Default(DelegationChangesTestData.WithResourceID("ttd-externalpdp-resource2"), DelegationChangesTestData.WithOfferedByPartyID(50005545), DelegationChangesTestData.WithToUuid(UuidType.SystemUser, Guid.Parse("47caea5b-a80b-4343-b1d3-31eb523a4e28"))),
                WithDefaultCondition("ttd-externalpdp-resource2", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50005545" }, new AttributeMatch { Id = XacmlRequestAttribute.SystemUserIdAttribute, Value = "47caea5b-a80b-4343-b1d3-31eb523a4e28" })),
            ConditionalAdd(
                DelegationChangesTestData.Default(DelegationChangesTestData.WithResourceID("org1/app1"), DelegationChangesTestData.WithResourceInstanceId("f8d3526c-596b-4322-a041-38a8925c2a82"), DelegationChangesTestData.WithFromUuid(UuidType.Organization, Guid.Parse("00000000-0000-0000-0005-000000005545")), DelegationChangesTestData.WithToUuid(UuidType.Person, Guid.Parse("00000000-0000-0000-0005-000000002625"))),
                WithDefaultCondition("org1/app1", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50005545" }, new AttributeMatch { Id = XacmlRequestAttribute.UserAttribute, Value = "20000517" })),
            ConditionalAdd(
                DelegationChangesTestData.Default(DelegationChangesTestData.WithResourceID("app_org1_app1"), DelegationChangesTestData.WithResourceInstanceId("f8d3526c-596b-4322-a041-38a8925c2a82"), DelegationChangesTestData.WithFromUuid(UuidType.Organization, Guid.Parse("00000000-0000-0000-0005-000000005545")), DelegationChangesTestData.WithToUuid(UuidType.Organization, Guid.Parse("00000000-0000-0000-0005-000000004222"))),
                WithDefaultCondition("org1/app1", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50005545" }, new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50004222" }),
                WithDefaultCondition("org1/app1", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50005545" }, new AttributeMatch { Id = XacmlRequestAttribute.UserAttribute, Value = "20000095" })),
            ConditionalAdd(
                DelegationChangesTestData.Default(DelegationChangesTestData.WithResourceID("ttd-externalpdp-resource3"), DelegationChangesTestData.WithResourceInstanceId("4c1b880e-f425-4050-9a46-cd1b3e56bf94"), DelegationChangesTestData.WithOfferedByPartyID(50005545), DelegationChangesTestData.WithCoveredByUserID(1337)),
                WithDefaultCondition("ttd-externalpdp-resource3", new AttributeMatch { Id = XacmlRequestAttribute.PartyAttribute, Value = "50005545" }, new AttributeMatch { Id = XacmlRequestAttribute.UserAttribute, Value = "1337" })),
        };

        var result = new List<DelegationChangeExternal>();
        foreach (var item in data)
        {
            item(input, result);
        }

        return Task.FromResult(result as IEnumerable<DelegationChangeExternal>);
    }

    public static Func<DelegationChangeInput, bool> WithDefaultCondition(string resourceId, AttributeMatch from, AttributeMatch to) => delegation =>
        (IfAltinnAppID(resourceId)(delegation) || IfResourceID(resourceId)(delegation)) &&
        IfFromMatch(from)(delegation) &&
        IfToMatch(to)(delegation);

    public static Func<DelegationChangeInput, bool> IfAltinnAppID(string appID) => delegation =>
    {
        var app = Strings.Join(
            [
                delegation.Resource.FirstOrDefault(resource => resource.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute)?.Value,
                delegation.Resource.FirstOrDefault(resource => resource.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute)?.Value
            ],
            "/");

        return app == appID;
    };

    public static Func<DelegationChangeInput, bool> IfResourceID(string resourceId) => delegation =>
    {
        return delegation.Resource.FirstOrDefault(resource => resource.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistry)?.Value == resourceId;
    };

    public static Func<DelegationChangeInput, bool> IfToMatch(AttributeMatch to) => delegation =>
        delegation.Subject.Id == to.Id && delegation.Subject.Value == to.Value;

    public static Func<DelegationChangeInput, bool> IfFromMatch(AttributeMatch from) => delegation =>
        delegation.Party.Id == from.Id && delegation.Party.Value == from.Value;

    public static Action<DelegationChangeInput, List<DelegationChangeExternal>> ConditionalAdd(DelegationChangeExternal data, params Func<DelegationChangeInput, bool>[] actions)
    {
        return (input, result) =>
        {
            if (actions.Any(condtion => condtion(input)))
            {
                result.Add(data);
            }
        };
    }

    public async Task<IEnumerable<DelegationChangeExternal>> GetAllDelegationChanges(CancellationToken cancellationToken, params Action<DelegationChangeInput>[] actions)
    {
        var result = new DelegationChangeInput();
        foreach (var action in actions)
        {
            action(result);
        }

        return await GetAllDelegationChanges(result);
    }

    public Task<IEnumerable<AuthorizedPartyDto>> GetAuthorizedParties(CancellationToken cancellationToken = default)
    {
        int? userId = _httpContextAccessor.HttpContext.User.GetUserIdAsInt();
        string authorizedPartiesPath = GetAuthorizedPartiesPath(userId.Value);
        if (File.Exists(authorizedPartiesPath))
        {
            string content = File.ReadAllText(authorizedPartiesPath);
            return Task.FromResult((IEnumerable<AuthorizedPartyDto>)JsonSerializer.Deserialize(content, typeof(IEnumerable<AuthorizedPartyDto>), _jsonOptions));
        }

        return Task.FromResult<IEnumerable<AuthorizedPartyDto>>([]);
    }

    private static string GetAuthorizedPartiesPath(int userId)
    {
        return Path.Combine("Data", "AccessManagement", "AuthorizedParties", $"{userId}.json");
    }
}