#nullable enable

using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.ResourceRegistry.Errors;

/// <summary>
/// A <see cref="ProblemDetails"/> for Altinn validation errors.
/// </summary>
internal class AltinnProblemDetails
    : ProblemDetails
{
    /// <summary>
    /// Creates a new instance of the <see cref="AltinnProblemDetails"/> class that represents a RR-001 error.
    /// </summary>
    /// <returns>The <see cref="AltinnProblemDetails"/>.</returns>
    public static AltinnProblemDetails AccessList_UpdateMembers_TooMany()
        => new("RR-001", HttpStatusCode.BadRequest, "Cannot replace more than 100 members at a time. Use POST and DELETE methods instead.");

    /// <summary>
    /// Creates a new instance of the <see cref="AltinnProblemDetails"/> class that represents a RR-002 error.
    /// </summary>
    /// <returns>The <see cref="AltinnProblemDetails"/>.</returns>
    public static AltinnProblemDetails AccessList_AddRemoveMembers_TooMany()
        => new("RR-002", HttpStatusCode.BadRequest, "Cannot add or remove more than 100 members at a time.");

    /// <summary>
    /// Creates a new instance of the <see cref="AltinnProblemDetails"/> class that represents a RR-003 error.
    /// </summary>
    /// <returns>The <see cref="AltinnProblemDetails"/>.</returns>
    public static AltinnProblemDetails PartyReference_NotFound()
        => new("RR-003", HttpStatusCode.BadRequest, "Party reference not found.");

    /// <summary>
    /// Creates a new instance of the <see cref="AltinnProblemDetails"/> class that represents a RR-004 error.
    /// </summary>
    /// <returns>The <see cref="AltinnProblemDetails"/>.</returns>
    public static AltinnProblemDetails AccessList_IncludeMembers_NotImplemented()
        => new("RR-004", HttpStatusCode.NotImplemented, "Members are not supported yet as an include option for this endpoint.");

    /// <summary>
    /// Creates a new instance of the <see cref="AltinnProblemDetails"/> class that represents a RR-005 error.
    /// </summary>
    /// <returns>The <see cref="AltinnProblemDetails"/>.</returns>
    public static AltinnProblemDetails AccessList_IncludeResourceConnections_MissingResourceIdentifier()
        => new("RR-005", HttpStatusCode.BadRequest, "Resource identifier is required when including resource connections.");

    private AltinnProblemDetails(string errorCode, HttpStatusCode status, string detail)
    {
        ErrorCode = errorCode;
        Status = (int)status;
        Detail = detail;
    }

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorCode { get; set; }
}
