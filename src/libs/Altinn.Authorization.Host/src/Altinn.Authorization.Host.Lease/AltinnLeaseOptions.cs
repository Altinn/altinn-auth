namespace Altinn.Authorization.Host.Lease;

/// <summary>
/// Represents configuration options for managing leases in the Altinn system.
/// Contains the lease type and, when applicable, configuration for using a storage account to store leases.
/// </summary>
public class AltinnLeaseOptions
{
    /// <summary>
    /// Gets or sets the type of lease to be used.
    /// This property defines whether the lease is stored in memory or in an Azure Storage Account.
    /// </summary>
    public AltinnLeaseType Type { get; set; }

    /// <summary>
    /// Gets or sets the configuration for using a storage account to store leases.
    /// This property is only relevant when the lease type is <see cref="AltinnLeaseType.AzureStorageAccount"/>.
    /// </summary>
    public StorageAccountLease StorageAccount { get; set; } = new();

    /// <summary>
    /// Represents the lease configuration specific to an Azure Storage Account.
    /// Contains the necessary details to access and configure the Azure storage account for lease management.
    /// </summary>
    public class StorageAccountLease
    {
        /// <summary>
        /// Gets or sets the URI endpoint for the Azure Storage Account.
        /// This endpoint is used to connect to the storage service where leases will be stored.
        /// </summary>
        public Uri BlobEndpoint { get; set; }

        /// <summary>
        /// The name of the storage account container used for storing leases.
        /// This is a static value used across all instances of <see cref="StorageAccountLease"/>.
        /// </summary>
        internal static string Name { get; set; } = "Leases";

        /// <summary>
        /// The name of the container in the Azure Storage Account that holds the lease data.
        /// This is a static value used to identify the specific container for lease storage.
        /// </summary>
        internal static string Container { get; set; } = "leases";
    }
}

/// <summary>
/// Defines the available transport types for storing and managing leases in the Altinn system.
/// The transport type determines where and how the lease data is managed.
/// </summary>
public enum AltinnLeaseType
{
    /// <summary>
    /// In-memory transport, primarily used for testing purposes.
    /// This type of lease is not persistent and will be lost if the application stops.
    /// </summary>
    InMemory = default,

    /// <summary>
    /// Azure Storage Account transport.
    /// This type uses Azure Storage Accounts to persist lease data across application restarts.
    /// </summary>
    AzureStorageAccount,
}
