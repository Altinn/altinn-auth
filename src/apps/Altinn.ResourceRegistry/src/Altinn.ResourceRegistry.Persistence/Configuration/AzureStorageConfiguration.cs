namespace Altinn.ResourceRegistry.Persistence.Configuration;

/// <summary>
/// Settings for Azure storage
/// </summary>
public class AzureStorageConfiguration
{
    /// <summary>
    /// The storage account name for the resource registry
    /// </summary>
    public required string ResourceRegistryAccountName { get; set; }

    /// <summary>
    /// The storage account key for the resource registry
    /// </summary>
    public required string ResourceRegistryAccountKey { get; set; }

    /// <summary>
    /// The name of the storage container in the resource registry storage account
    /// </summary>
    public required string ResourceRegistryContainer { get; set; }

    /// <summary>
    /// The url for the blob end point for the resource registry
    /// </summary>
    public required string ResourceRegistryBlobEndpoint { get; set; }

    /// <summary>
    /// The storage account name for Metadata
    /// </summary>
    public required string MetadataAccountName { get; set; }

    /// <summary>
    /// The storage account key for Metadata
    /// </summary>
    public required string MetadataAccountKey { get; set; }

    /// <summary>
    /// The name of the storage container in the Metadata storage account
    /// </summary>
    public required string MetadataContainer { get; set; }

    /// <summary>
    /// The url for the blob end point for Metadata
    /// </summary>
    public required string MetadataBlobEndpoint { get; set; }

    /// <summary>
    /// The blob lease timeout value in seconds
    /// </summary>
    public int BlobLeaseTimeout { get; set; }
}
