#nullable enable

using Altinn.Urn;

namespace Altinn.Authorization.Api.Contracts.Authorization;

/// <summary>
/// A unique reference to a role in the form of an URN.
/// </summary>
[KeyValueUrn]
public abstract partial record RoleUrn
{
    /// <summary>
    /// Try to get the urn as a legacy role code. E.g. urn:altinn:rolecode:priv or urn:altinn:rolecode:seln or urn:altinn:rolecode:dagl
    /// </summary>
    /// <param name="legacyRoleCode">The resulting role code.</param>
    /// <returns><see langword="true"/> if this is a legacy role code, otherwise <see langword="false"/>.</returns>
    [UrnKey("altinn:rolecode")]
    public partial bool IsLegacyRole(out LegacyRoleCode legacyRoleCode);

    /// <summary>
    /// Try to get the urn as an altinn role code. E.g. urn:altinn:role:privatperson or urn:altinn:role:selvregistrert
    /// </summary>
    /// <param name="altinnRoleCode">The resulting altinn role code.</param>
    /// <returns><see langword="true"/> if this is a role code, otherwise <see langword="false"/>.</returns>
    [UrnKey("altinn:role")]
    public partial bool IsAltinnRole(out AltinnRoleCode altinnRoleCode);

    /// <summary>
    /// Try to get the urn as a ccr (enhetsregisteret) role. E.g. urn:altinn:external-role:ccr:daglig-leder
    /// </summary>
    /// <param name="ccrRoleCode">The resulting ccr (enhetsregisteret) role.</param>
    /// <returns><see langword="true"/> if this is a ccr (enhetsregisteret) role, otherwise <see langword="false"/>.</returns>
    [UrnKey("altinn:external-role:ccr")]
    public partial bool IsCcrRole(out CcrRoleCode ccrRoleCode);

    /// <summary>
    /// Try to get the urn as a cra (sivilrettsforvaltningen) role. E.g. urn:altinn:external-role:cra:helfo-fastlege
    /// </summary>
    /// <param name="craRoleCode">The resulting cra (sivilrettsforvaltningen) role.</param>
    /// <returns><see langword="true"/> if this is a cra (sivilrettsforvaltningen) role, otherwise <see langword="false"/>.</returns>
    [UrnKey("altinn:external-role:cra")]
    public partial bool IsCraRole(out CraRoleCode craRoleCode);
}
