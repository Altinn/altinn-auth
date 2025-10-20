using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Altinn.Register.Contracts;

namespace Altinn.Authorization.Integration.Platform.Register;

/// <summary>
/// Client for interacting with endpoint that stream roles (ER-roles).
/// </summary>
public partial class AltinnRegisterClient
{
    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<PlatformResponse<PageStream<ExternalRoleAssignmentEvent>>>> StreamRoles(IEnumerable<string> fields, string nextPage = null, CancellationToken cancellationToken = default)
    {
        List<Action<HttpRequestMessage>> request = [
            RequestComposer.WithHttpVerb(HttpMethod.Get),
            RequestComposer.WithSetUri(RegisterOptions.Value.Endpoint, "/register/api/v2/internal/parties/external-roles/assignments/events/stream"),
            RequestComposer.WithSetUri(nextPage),
            RequestComposer.WithAppendQueryParam("fields", fields),
            RequestComposer.WithPlatformAccessToken(async () => await TokenGenerator.CreatePlatformAccessToken(cancellationToken))
        ];

        var response = await HttpClient.SendAsync(RequestComposer.New([.. request]), cancellationToken);

        return new PaginatorStream<ExternalRoleAssignmentEvent>(HttpClient, response, request);
    }
}

/// <summary>
/// Represents a party with various personal and organizational details.
/// This model is used to deserialize JSON responses containing party information.
/// </summary>
/// <summary>
/// Represents an external role assignment event.
/// </summary>
[DebuggerDisplay("{Type,nq} {RoleIdentifier,nq} ({RoleSource,nq}) from {FromParty} to {ToParty}")]
[ExcludeFromCodeCoverage]
public sealed record ExternalRoleAssignmentEvent
{
    /// <summary>
    /// Gets the version ID of the event.
    /// </summary>
    [JsonPropertyName("versionId")]
    public required ulong VersionId { get; init; }

    /// <summary>
    /// Gets the type of the event.
    /// </summary>
    [JsonPropertyName("type")]
    public required EventType Type { get; init; }

    /// <summary>
    /// Gets the role source of the event.
    /// </summary>
    [JsonPropertyName("roleSource")]
    public required ExternalRoleSource RoleSource { get; init; }

    /// <summary>
    /// Gets the role identifier of the event.
    /// </summary>
    [JsonPropertyName("roleIdentifier")]
    public required string RoleIdentifier { get; init; }

    /// <summary>
    /// Gets the party the role is assigned to.
    /// </summary>
    [JsonPropertyName("toParty")]
    public required Guid ToParty { get; init; }

    /// <summary>
    /// Gets the party the role is assigned from.
    /// </summary>
    [JsonPropertyName("fromParty")]
    public required Guid FromParty { get; init; }

    /// <summary>
    /// Role-assignment event type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<EventType>))]
    public enum EventType
    {
        /// <summary>
        /// The role-assignment was added.
        /// </summary>
        Added,

        /// <summary>
        /// The role-assignment was removed.
        /// </summary>
        Removed,
    }
}
