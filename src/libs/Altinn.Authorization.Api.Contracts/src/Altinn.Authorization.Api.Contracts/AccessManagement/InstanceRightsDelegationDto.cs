namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Input model for instance rights delegation supporting both existing connections and new person assignments
/// </summary>
public class InstanceRightsDelegationDto
{
    /// <summary>
    /// Target person for delegation. Used when creating a new rightholder connection.
    /// Mutually exclusive with using 'to' query parameter.
    /// </summary>
    public PersonInputDto? To { get; set; }

    /// <summary>
    /// List of right keys to delegate for the instance
    /// </summary>
    public IEnumerable<string> DirectRightKeys { get; set; } = [];
}

/// <summary>
/// Person input for creating new rightholder connections
/// </summary>
public class PersonInputDto
{
    /// <summary>
    /// Person identifier - either 11-digit national identity number or username
    /// </summary>
    public string PersonIdentifier { get; set; }

    /// <summary>
    /// Last name of the person
    /// </summary>
    public string LastName { get; set; }
}
