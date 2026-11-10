#nullable enable

using Microsoft.AspNetCore.Authorization;

namespace Altinn.ResourceRegistry.Auth;

/// <summary>
/// Authorization requirement for checking if the user owns the resource
/// </summary>
public class UserOwnsResourceRequirement
    : IAuthorizationRequirement
{
    /// <summary>
    /// Singleton instance of the requirement.
    /// </summary>
    public static UserOwnsResourceRequirement Instance { get; } = new();

    private UserOwnsResourceRequirement()
    {
    }
}
