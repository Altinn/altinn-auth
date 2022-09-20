namespace Altinn.AccessGroups.Persistance
{
    /// <summary>
    /// Settings for Azure storage
    /// </summary>
    public class AzureStorageConfiguration
    {
        /// <summary>
        /// The storage account name for the resource registry
        /// </summary>
        public string ResourceRegistryAccountName { get; set; }

        /// <summary>
        /// The storage account key for the resource registry
        /// </summary>
        public string ResourceRegistryAccountKey { get; set; }

        /// <summary>
        /// The name of the storage container in the resource registry storage account
        /// </summary>
        public string ResourceRegistryContainer { get; set; }

        /// <summary>
        /// The url for the blob end point for the resource registry
        /// </summary>
        public string ResourceRegistryBlobEndpoint { get; set; }

        /// <summary>
        /// The blob lease timeout value in seconds
        /// </summary>
        public int BlobLeaseTimeout { get; set; }
    }
}
