namespace Altinn.Authorization.Host.Lease.Appsettings;

/// <summary>
/// Represents the lease settings used for distributed locking in Altinn authorization.
/// This class includes configuration settings for external storage used in lease management,
/// such as Azure Blob Storage.
/// </summary>
public class LeaseSettings
{
    /// <summary>
    /// Gets or sets the storage account settings used for lease management.
    /// </summary>
    public StorageAccountSettings StorageAccount { get; set; }

    /// <summary>
    /// Represents storage account configuration for lease management.
    /// </summary>
    public class StorageAccountSettings
    {
        /// <summary>
        /// Gets or sets the Azure Blob Storage endpoint URI.
        /// This is used for distributed lease storage.
        /// </summary>
        public Uri BlobEndpoint { get; set; }
    }
}
