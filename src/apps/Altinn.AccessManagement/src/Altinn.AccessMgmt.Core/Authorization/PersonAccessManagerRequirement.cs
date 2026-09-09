using Microsoft.AspNetCore.Authorization;

namespace Altinn.AccessMgmt.Core.Authorization;

/// <summary>
/// Authorization requirement that restricts access managers of a person from accessing
/// the "from-others" direction (incoming connections) in the Connections API.
/// When the authenticated user is not the party and the party is a person,
/// only outgoing connections (to-others) are allowed.
/// </summary>
public sealed class PersonAccessManagerRequirement : IAuthorizationRequirement
{
}
