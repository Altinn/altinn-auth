using System.Text.Json.Serialization;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;

namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Action
/// </summary>
public class ActionDto
{
    /// <summary>
    /// Key that uniquely identifies an action with all parameters found in the policy
    /// </summary>
    public required string ActionKey { get; set; }

    /// <summary>
    /// Name of the action to present in frontend
    /// </summary>
    public required string ActionName { get; set; }

    /// <summary>
    /// Result of the delegation check.
    /// True if the user is authorized to give others access to the resource on behalf of the specified party, false otherwise.
    /// </summary>
    public required bool Result { get; set; }

    /// <summary>
    /// Reason for the result of the delegation check.
    /// </summary>
    public IEnumerable<Reason> Reasons { get; set; } = [];

    /// <summary>
    /// Resource delegation check response model
    /// </summary>
    public class Reason
    {
        /// <summary>
        /// Description of the reason.
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Reason  code indicating the type of delegation check result.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required DelegationCheckReasonCode ReasonKey { get; set; }

        /// <summary>
        /// Role ID of the role providing access
        /// </summary>
        public Guid? RoleId { get; set; }

        /// <summary>
        /// Role URN of the role providing access
        /// </summary>
        public string? RoleUrn { get; set; }

        /// <summary>
        /// Package ID of the package providing access
        /// </summary>
        public Guid? PackageId { get; set; }

        /// <summary>
        /// Package URN of the package providing access
        /// </summary>
        public string? PackageUrn { get; set; }

        /// <summary>
        /// From party ID of the providing access
        /// </summary>
        public Guid? FromId { get; set; }

        /// <summary>
        /// Name of the party providing access
        /// </summary>
        public string? FromName { get; set; }

        /// <summary>
        /// To party ID of the  providing access
        /// </summary>
        public Guid? ToId { get; set; }

        /// <summary>
        /// Name of the party providing access
        /// </summary>
        public string? ToName { get; set; }

        /// <summary>
        /// Via party ID of the keyrole party the user has inherited access through
        /// </summary>
        public Guid? ViaId { get; set; }

        /// <summary>
        /// Name of the party the user has inherited access through
        /// </summary>
        public string? ViaName { get; set; }

        /// <summary>
        /// Role ID for the keyrole the user has for the ViaId party
        /// </summary>
        public Guid? ViaRoleId { get; set; }

        /// <summary>
        /// Role URN for the keyrole the user has for the ViaId party
        /// </summary>
        public string? ViaRoleUrn { get; set; }
    }
}
