using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.SblBridge;

/// <summary>
/// Client for interacting with endpoint that stream roles (ER-roles).
/// </summary>
public partial class AltinnSblBridgeClient
{
    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<PlatformResponse<PageStream<RoleDelegationModel>>>> StreamRoles(string subscriptionId, string nextPage = null, CancellationToken cancellationToken = default)
    {
        List<Action<HttpRequestMessage>> request = [
            RequestComposer.WithHttpVerb(HttpMethod.Get),
            RequestComposer.WithSetUri(Options.Value.Endpoint, $"roledelegationevent/api/getevents?subscriptionId={subscriptionId}"),
            RequestComposer.WithSetUri(nextPage)
        ];

        var response = await HttpClient.SendAsync(RequestComposer.New([.. request]), cancellationToken);

        return new PaginatorStream<RoleDelegationModel>(HttpClient, response, request);
    }
}

/// <summary>
/// Represents a party with various personal and organizational details.
/// This model is used to deserialize JSON responses containing party information.
/// </summary>
[ExcludeFromCodeCoverage]
public class RoleDelegationModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the Altinn role delegation event.
    /// </summary>
    [JsonPropertyName("altinnRoleDelegationEventId")]
    public long AltinnRoleDelegationEventId { get; set; }

    /// <summary>
    /// Gets or sets the action taken in the delegation (Delegation/Revoke).
    /// </summary>
    [JsonPropertyName("delegationAction")]
    public DelegationAction DelegationAction { get; set; }

    /// <summary>
    /// Gets or sets the role type code.
    /// </summary>
    [JsonPropertyName("roleTypeCode")]
    public string RoleTypeCode { get; set; }

    /// <summary>
    /// Gets or sets the ID of the party from which the role is delegated.
    /// </summary>
    [JsonPropertyName("fromPartyId")]
    public int FromPartyId { get; set; }

    /// <summary>
    /// Gets or sets the UUID of the party from which the role is delegated.
    /// </summary>
    [JsonPropertyName("fromPartyUuid")]
    public Guid FromPartyUuid { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user to whom the role is delegated.
    /// </summary>
    [JsonPropertyName("toUserId")]
    public int? ToUserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the party to which the role is delegated.
    /// </summary>
    [JsonPropertyName("toPartyId")]
    public int? ToPartyId { get; set; }

    /// <summary>
    /// Gets or sets the UUID of the user party to which the role is delegated.
    /// </summary>
    [JsonPropertyName("toUserPartyUuid")]
    public Guid? ToUserPartyUuid { get; set; }

    [JsonPropertyName("toUserType")]
    public UserType ToUserType { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the delegation change occurred.
    /// </summary>
    [JsonPropertyName("delegationChangeDateTime")]
    public DateTimeOffset? DelegationChangeDateTime { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who performed the delegation.
    /// </summary>
    [JsonPropertyName("performedByUserId")]
    public int? PerformedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the party who performed the delegation.
    /// </summary>
    [JsonPropertyName("performedByPartyId")]
    public int? PerformedByPartyId { get; set; }

    /// <summary>
    /// Gets or sets the UUID of the user who performed the delegation.
    /// </summary>
    [JsonPropertyName("performedByUserUuid")]
    public Guid? PerformedByUserUuid { get; set; }

    /// <summary>
    /// Gets or sets the UUID of the party who performed the delegation.
    /// </summary>
    [JsonPropertyName("performedByPartyUuid")]
    public Guid? PerformedByPartyUuid { get; set; }

    /// <summary>
    /// Gets or sets the ID of the role.
    /// </summary>
    [JsonPropertyName("roleId")]
    public int? RoleId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the role delegation.
    /// </summary>
    [JsonPropertyName("roleDelegationId")]
    public int? RoleDelegationId { get; set; }
}

/// <summary>
/// Enumeration for which delegation action was performed
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DelegationAction>))]
public enum DelegationAction : int
{
    /// <summary>
    /// Value indicating no value has been set
    /// </summary>
    [JsonStringEnumMemberName("NotSet")]
    NotSet = 0,

    /// <summary>
    /// Value indicating that this was a delegation
    /// </summary>
    [JsonStringEnumMemberName("Delegate")]
    Delegate = 1,

    /// <summary>
    /// Value indicating that this was a revoke
    /// </summary>
    [JsonStringEnumMemberName("Revoke")]
    Revoke = 2,

    /// <summary>
    /// Value indicating that this was a delete based on a soft delete of a DelegationScheme
    /// </summary>
    [JsonStringEnumMemberName("DelegationSchemeCascadingDelete")]
    DelegationSchemeCascadingDelete = 3
}

/// <summary>
///  User Type is used to specify the type of the user i.e whether SSN,Self,enterprise identified or agency User.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UserType>))]
public enum UserType : int
{
    /// <summary>
    /// User type has not been specified
    /// </summary>
    [EnumMember]
    None = 0,

    /// <summary>
    /// User Type is SSN Identified User.
    /// </summary>
    [EnumMember]
    SSNIdentified = 1,

    /// <summary>
    /// User Type is Self Identified User.
    /// </summary>
    [EnumMember]
    SelfIdentified = 2,

    /// <summary>
    /// User Type is EnterpriseIdentified Identified User.
    /// </summary>
    [EnumMember]
    EnterpriseIdentified = 3,

    /// <summary>
    /// User Type is Agency User
    /// </summary>
    [EnumMember]
    AgencyUser = 4,

    /// <summary>
    /// User Type is PSAN User
    /// </summary>
    [EnumMember]
    PSAN = 5,

    /// <summary>
    /// User Type is PSA User
    /// </summary>
    [EnumMember]
    PSA = 6
}
