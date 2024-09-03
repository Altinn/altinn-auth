#nullable enable

using System.Net;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.ResourceRegistry.Core.Errors;

/// <summary>
/// Problem descriptors for the Resource Registry.
/// </summary>
public static class Problems
{
    private static readonly ProblemDescriptorFactory _factory
        = ProblemDescriptorFactory.New("RR");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor AccessList_IncludeMembers_NotImplemented { get; }
        = _factory.Create(0, HttpStatusCode.NotImplemented, "Members are not supported yet as an include option for this endpoint.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor PartyReference_NotFound { get; }
        = _factory.Create(1, HttpStatusCode.BadRequest, "One or more party references not found.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor ResourceReference_NotFound { get; }
        = _factory.Create(2, HttpStatusCode.BadRequest, "One or more resource references not found.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor IncorrectMatchingOrgForResource { get; }
        = _factory.Create(3, HttpStatusCode.BadRequest, "Org does not match with resource.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor AccessList_References_OtherServiceOwners_Resource { get; }
        = _factory.Create(4, HttpStatusCode.Forbidden, "Cannot create a access-list connection to a resource not owned by the same service-owner as the access-list.");
}
