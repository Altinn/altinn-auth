namespace Altinn.Authorization.Models.ResourceRegistry;

/// <summary>
/// The reference
/// </summary>
public class AuthorizationReferenceAttribute
{
    /// <summary>
    /// The key for authorization reference. Used for authorization api related to resource
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The value for authorization reference. Used for authorization api related to resource
    /// </summary>
    public string Value { get; set; }
}
