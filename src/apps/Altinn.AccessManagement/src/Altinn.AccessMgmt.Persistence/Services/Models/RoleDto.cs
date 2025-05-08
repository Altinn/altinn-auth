using Altinn.AccessMgmt.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Models;

/// <summary>
/// Represents a group of areas, categorized under a specific entity type.
/// </summary>
public class RoleDto
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name
    /// e.g Dagligleder
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Code
    /// e.g daglig-leder
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Description
    /// e.g The main operator of the organization
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Defines the role as a KeyRole
    /// </summary>
    public bool IsKeyRole { get; set; }

    /// <summary>
    /// Urn
    /// e.g altinn:external-role:ccr:daglig-leder
    /// altinn:role:tilgangsstyrer
    /// </summary>
    public string Urn { get; set; }

    /// <summary>
    /// Construct from Role
    /// </summary>
    /// <param name="role"><see cref="Role"/>Role</param>
    public RoleDto(Role role)
    {
        Id = role.Id;
        Name = role.Name;
        Urn = role.Urn;
        Code = role.Code;
        Description = role.Description;
        IsKeyRole = role.IsKeyRole;
    }

    /// <summary>
    /// Construct from Role
    /// </summary>
    /// <param name="role"><see cref="Role"/>Role</param>
    public RoleDto(ExtRole role)
    {
        Id = role.Id;
        Name = role.Name;
        Urn = role.Urn;
        Code = role.Code;
        Description = role.Description;
        IsKeyRole = role.IsKeyRole;
    }

    /// <summary>
    /// Construct from Role
    /// </summary>
    public RoleDto() { }
}
