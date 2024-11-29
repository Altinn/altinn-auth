#nullable enable

using System.Net;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.Authorization.Errors;

/// <summary>
/// Problem descriptors for Authorization
/// </summary>
public static class Problems
{
    private static readonly ProblemDescriptorFactory _factory
        = ProblemDescriptorFactory.New("AUTHZ");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/> for not implemented feature.
    /// </summary>
    public static ProblemDescriptor NotImplemented { get; }
        = _factory.Create(0, HttpStatusCode.NotImplemented, "Not implemented.");
}
