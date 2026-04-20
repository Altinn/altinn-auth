using NpgsqlTypes;

namespace Altinn.AccessManagement.Enums.ResourceRegistry
{
    /// <summary>
    /// Enum representation of the different types of resources supported by the resource registry
    /// </summary>
    [Flags]
    public enum ResourceTypeExternal
    {
        [PgName("default")]
        Default = 0,

        [PgName("systemresource")]
        Systemresource = 1,

        [PgName("maskinportenschema")]
        MaskinportenSchema = 2,

        [PgName("altinn2service")]
        Altinn2Service = 4,

        [PgName("altinnapp")]
        AltinnApp = 8,

        [PgName("genericaccessresource")]
        GenericAccessResource = 16,

        [PgName("brokerservice")]
        BrokerService = 1 << 5,

        [PgName("correspondenceservice")]
        CorrespondenceService = 1 << 6,

        [PgName("consent")]
        Consent = 1 << 7,

        [PgName("migratedapp")]
        MigratedApp = 1 << 8,
    }
}
