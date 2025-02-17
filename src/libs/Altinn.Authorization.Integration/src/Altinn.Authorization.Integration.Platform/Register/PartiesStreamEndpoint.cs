using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Register;

/// <summary>
/// Client for interacting with endpoint that stream parties (organization, users and systemusers).
/// </summary>
public partial class RegisterClient
{
    /// <summary>
    /// List of available fields that can be retrieved from the register.
    /// </summary>
    private readonly IEnumerable<string> _availableFields = [
        "party",
        "organization",
        "person",
        "identifiers",
        "party-uuid",
        "party-version-id",
        "party-is-deleted",
        "organization-business-address",
        "organization-mailing-address",
        "organization-internet-address",
        "organization-email-address",
        "organization-fax-number",
        "organization-mobile-number",
        "organization-telephone-number",
        "organization-unit-type",
        "organization-unit-status",
        "person-date-of-death",
        "person-mailing-address",
        "person-address",
        "person-last-name",
        "person-middle-name",
        "person-first-name",
        "party-modified-at",
        "party-created-at",
        "party-organization-identifier",
        "party-person-identifier",
        "party-name",
        "party-type",
        "party-id",
        "person-date-of-birth",
        "sub-units"
    ];

    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<PlatformResponse<PageStream<PartyModel>>>> StreamParties(IEnumerable<string> fields, string nextPage = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<Action<HttpRequestMessage>> request = [
            RequestComposer.WithHttpVerb(HttpMethod.Get),
            RequestComposer.WithSetUri(Options.Value.Endpoint, "/register/api/v2/internal/parties/stream"),
            RequestComposer.WithSetUri(nextPage),
            RequestComposer.WithAppendQueryParam("fields", fields),
            RequestComposer.WithPlatformAccessToken(AccessTokenGenerator, "access-management")
        ];

        var response = await HttpClient.SendAsync(RequestComposer.New([.. request]), cancellationToken);

        return new PaginatorStream<PartyModel>(HttpClient, response, request);
    }
}

/// <summary>
/// Represents a party with various personal and organizational details.
/// This model is used to deserialize JSON responses containing party information.
/// </summary>
[ExcludeFromCodeCoverage]
public class PartyModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the party.
    /// </summary>
    [JsonPropertyName("partyUuid")]
    public string PartyUuid { get; set; }

    /// <summary>
    /// Gets or sets the numeric identifier for the party.
    /// </summary>
    [JsonPropertyName("partyId")]
    public int PartyId { get; set; }

    /// <summary>
    /// Gets or sets the type of the party (e.g., Person, Organization).
    /// </summary>
    [JsonPropertyName("partyType")]
    public string PartyType { get; set; }

    /// <summary>
    /// Gets or sets the name of the party.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the person identifier (if applicable).
    /// </summary>
    [JsonPropertyName("personIdentifier")]
    public string PersonIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the organization identifier (if applicable).
    /// </summary>
    [JsonPropertyName("organizationIdentifier")]
    public string OrganizationIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the party was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the party was last modified.
    /// </summary>
    [JsonPropertyName("modifiedAt")]
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the party is marked as deleted.
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the version identifier for the party data.
    /// </summary>
    [JsonPropertyName("versionId")]
    public int VersionId { get; set; }

    /// <summary>
    /// Gets or sets the first name of the party (if applicable).
    /// </summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the middle name of the party (if applicable).
    /// </summary>
    [JsonPropertyName("middleName")]
    public string MiddleName { get; set; }

    /// <summary>
    /// Gets or sets the last name of the party (if applicable).
    /// </summary>
    [JsonPropertyName("lastName")]
    public string LastName { get; set; }

    /// <summary>
    /// Gets or sets the address information of the party.
    /// </summary>
    [JsonPropertyName("address")]
    public PartyAddress Address { get; set; }

    /// <summary>
    /// Gets or sets the mailing address information of the party.
    /// </summary>
    [JsonPropertyName("mailingAddress")]
    public PartyMailingAddress MailingAddress { get; set; }

    /// <summary>
    /// Gets or sets the date of birth of the party (if applicable).
    /// </summary>
    [JsonPropertyName("dateOfBirth")]
    public string DateOfBirth { get; set; }

    /// <summary>
    /// Gets or sets the date of death of the party (if applicable).
    /// </summary>
    [JsonPropertyName("dateOfDeath")]
    public string DateOfDeath { get; set; }

    /// <summary>
    /// Represents the address information of the party.
    /// </summary>
    public class PartyAddress
    {
        /// <summary>
        /// Gets or sets the municipal number of the party's address.
        /// </summary>
        [JsonPropertyName("municipalNumber")]
        public string MunicipalNumber { get; set; }

        /// <summary>
        /// Gets or sets the name of the municipality for the party's address.
        /// </summary>
        [JsonPropertyName("municipalName")]
        public string MunicipalName { get; set; }

        /// <summary>
        /// Gets or sets the street name of the party's address.
        /// </summary>
        [JsonPropertyName("streetName")]
        public string StreetName { get; set; }

        /// <summary>
        /// Gets or sets the house number of the party's address.
        /// </summary>
        [JsonPropertyName("houseNumber")]
        public string HouseNumber { get; set; }

        /// <summary>
        /// Gets or sets the house letter (if applicable) of the party's address.
        /// </summary>
        [JsonPropertyName("houseLetter")]
        public string HouseLetter { get; set; }

        /// <summary>
        /// Gets or sets the postal code of the party's address.
        /// </summary>
        [JsonPropertyName("postalCode")]
        public string PostalCode { get; set; }

        /// <summary>
        /// Gets or sets the city of the party's address.
        /// </summary>
        [JsonPropertyName("city")]
        public string City { get; set; }
    }

    /// <summary>
    /// Represents the mailing address information of the party.
    /// </summary>
    public class PartyMailingAddress
    {
        /// <summary>
        /// Gets or sets the mailing address of the party.
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the postal code for the mailing address of the party.
        /// </summary>
        [JsonPropertyName("postalCode")]
        public string PostalCode { get; set; }

        /// <summary>
        /// Gets or sets the city for the mailing address of the party.
        /// </summary>
        [JsonPropertyName("city")]
        public string City { get; set; }
    }
}
