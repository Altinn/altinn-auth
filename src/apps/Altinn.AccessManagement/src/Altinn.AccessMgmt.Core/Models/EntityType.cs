namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// EntityType
/// </summary>
public class EntityType
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ProviderId
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }
}

/// <summary>
/// Extended EntityType
/// </summary>
public class ExtEntityType : EntityType
{
    /// <summary>
    /// Provider
    /// </summary>
    public Provider Provider { get; set; }
}

/// <summary>
/// EntityType Ids
/// </summary>
public static class EntityTypeId
{
    /// <summary>
    /// Internal
    /// </summary>
    public static Guid Internal { get; } = Guid.Parse("4557CC81-C10D-40B4-8134-F8825060016E");

    /// <summary>
    /// Organization
    /// </summary>
    public static Guid Organization { get; } = Guid.Parse("8C216E2F-AFDD-4234-9BA2-691C727BB33D");

    /// <summary>
    /// Person
    /// </summary>
    public static Guid Person { get; } = Guid.Parse("BFE09E70-E868-44B3-8D81-DFE0E13E058A");

    /// <summary>
    /// SystemUser
    /// </summary>
    public static Guid SystemUser { get; } = Guid.Parse("FE643898-2F47-4080-85E3-86BF6FE39630");
}
