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
    public static readonly IEnumerable<string> AvailableFields = [
        "party",
        "org",
        "person",
        "identifiers",
        "person.name",
        "org.fax",
        "org.internet",
        "org.mobile",
        "person.mailing-address",
        "org.type",
        "org.status",
        "org.mailing-address",
        "org.business-address",
        "person.date-of-death",
        "person.date-of-birth",
        "org.email",
        "org.telephone",
        "uuid",
        "person.short-name",
        "person.last-name",
        "person.middle-name",
        "person.first-name",
        "version",
        "deleted",
        "modified",
        "created",
        "org-id",
        "person-id",
        "display-name",
        "type",
        "id",
        "person.address"
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
    [JsonPropertyName("partyType")]
    public string PartyType { get; set; }

    [JsonPropertyName("partyUuid")]
    public string PartyUuid { get; set; }

    [JsonPropertyName("partyId")]
    public int PartyId { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    [JsonPropertyName("personIdentifier")]
    public string PersonIdentifier { get; set; }

    [JsonPropertyName("organizationIdentifier")]
    public string OrganizationIdentifier { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("modifiedAt")]
    public DateTime ModifiedAt { get; set; }

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }

    [JsonPropertyName("versionId")]
    public int VersionId { get; set; }

    [JsonPropertyName("unitStatus")]
    public string UnitStatus { get; set; }

    [JsonPropertyName("unitType")]
    public string UnitType { get; set; }

    [JsonPropertyName("telephoneNumber")]
    public object TelephoneNumber { get; set; }

    [JsonPropertyName("mobileNumber")]
    public object MobileNumber { get; set; }

    [JsonPropertyName("faxNumber")]
    public object FaxNumber { get; set; }

    [JsonPropertyName("emailAddress")]
    public object EmailAddress { get; set; }

    [JsonPropertyName("internetAddress")]
    public object InternetAddress { get; set; }

    [JsonPropertyName("mailingAddress")]
    public PartyMailingAddress MailingAddress { get; set; }

    [JsonPropertyName("businessAddress")]
    public PartyBusinessAddress BusinessAddress { get; set; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }

    [JsonPropertyName("middleName")]
    public object MiddleName { get; set; }

    [JsonPropertyName("lastName")]
    public string LastName { get; set; }

    [JsonPropertyName("shortName")]
    public string ShortName { get; set; }

    [JsonPropertyName("address")]
    public PartyAddress Address { get; set; }

    [JsonPropertyName("dateOfBirth")]
    public string DateOfBirth { get; set; }

    [JsonPropertyName("dateOfDeath")]
    public string DateOfDeath { get; set; }

    public class PartyAddress
    {
        [JsonPropertyName("municipalNumber")]
        public string MunicipalNumber { get; set; }

        [JsonPropertyName("municipalName")]
        public string MunicipalName { get; set; }

        [JsonPropertyName("streetName")]
        public string StreetName { get; set; }

        [JsonPropertyName("houseNumber")]
        public string HouseNumber { get; set; }

        [JsonPropertyName("houseLetter")]
        public object HouseLetter { get; set; }

        [JsonPropertyName("postalCode")]
        public string PostalCode { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }
    }

    public class PartyBusinessAddress
    {
        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("postalCode")]
        public string PostalCode { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }
    }

    public class PartyMailingAddress
    {
        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("postalCode")]
        public string PostalCode { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }
    }
}
