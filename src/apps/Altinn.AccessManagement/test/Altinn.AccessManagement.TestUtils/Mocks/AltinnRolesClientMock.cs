using System.Text.Json;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Authorization.Platform.Authorization.Models;

namespace Altinn.AccessManagement.TestUtils.Mocks;

/// <summary>
/// Mock class for <see cref="IAltinnRolesClient"></see> interface
/// </summary>
public class AltinnRolesClientMock : IAltinnRolesClient
{
    private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnRolesClientMock"/> class
    /// </summary>
    public AltinnRolesClientMock()
    {
    }

    /// <inheritdoc/>
    public async Task<List<Role>> GetDecisionPointRolesForUser(int coveredByUserId, int offeredByPartyId, CancellationToken cancellationToken = default)
    {
        List<Role> roles = new List<Role>();
        string rolesPath = GetRolesPath(coveredByUserId, offeredByPartyId);
        if (File.Exists(rolesPath))
        {
            string content = await File.ReadAllTextAsync(rolesPath, cancellationToken);
            roles = (List<Role>)JsonSerializer.Deserialize(content, typeof(List<Role>), jsonOptions);
        }

        return roles;
    }

    /// <inheritdoc/>
    public async Task<List<Role>> GetRolesForDelegation(int coveredByUserId, int offeredByPartyId, CancellationToken cancellationToken = default)
    {
        List<Role> roles = new List<Role>();
        string rolesPath = GetRolesForDelegationPath(coveredByUserId, offeredByPartyId);
        if (File.Exists(rolesPath))
        {
            string content = await File.ReadAllTextAsync(rolesPath, cancellationToken);
            roles = (List<Role>)JsonSerializer.Deserialize(content, typeof(List<Role>), jsonOptions);
        }

        return roles;
    }

    private static string GetRolesPath(int coveredByUserId, int offeredByPartyId)
    {
        return TestDataDirectory.Combine("Roles", $"user_{coveredByUserId}", $"party_{offeredByPartyId}", "roles.json");
    }

    private static string GetRolesForDelegationPath(int coveredByUserId, int offeredByPartyId)
    {
        return TestDataDirectory.Combine("RolesForDelegation", $"user_{coveredByUserId}", $"party_{offeredByPartyId}", "roles.json");
    }
}
