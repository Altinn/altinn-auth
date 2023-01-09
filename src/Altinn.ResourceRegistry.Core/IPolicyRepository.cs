using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;

namespace Altinn.ResourceRegistry.Core
{
    /// <summary>
    /// Interface for operations on policy files.
    /// </summary>
    public interface IPolicyRepository
    {
        /// <summary>
        /// Gets file stream for the policy file from blob storage, if it exists at the specified path.
        /// </summary>
        /// <param name="resourceId">The resource id</param>
        /// <returns>File stream of the policy file</returns>
        Task<Stream> GetPolicyAsync(string resourceId);

        /// <summary>
        /// Gets file stream for the specified version of a policy file from blob storage, if it exists at the specified path.
        /// </summary>
        /// <param name="resourceId">The resource id</param>
        /// <param name="version">The blob storage version</param>
        /// <returns>File stream of the policy file</returns>
        Task<Stream> GetPolicyVersionAsync(string resourceId, string version);

        /// <summary>
        /// Writes a file stream to blobstorage to the specified path.
        /// </summary>
        /// <param name="resourceId">The resourceId</param> 
        /// <param name="fileStream">File stream of the policy file to be written</param>
        /// <returns>Azure response BlobContentInfo</returns>
        Task<Response<BlobContentInfo>> WritePolicyAsync(string resourceId, Stream fileStream);

        /// <summary>
        /// Writes a file stream to blobstorage to the specified path, including the conditional check that the provided blob lease id is valid.
        /// </summary>
        /// <param name="resourceId">The resourceId</param> 
        /// <param name="fileStream">File stream of the policy file to be written</param>
        /// <param name="blobLeaseId">The blob lease id, required to be able to write after a lock</param>
        /// <returns>Azure response BlobContentInfo</returns>
        Task<Response<BlobContentInfo>> WritePolicyConditionallyAsync(string resourceId, Stream fileStream, string blobLeaseId);

        /// <summary>
        /// Deletes a specific version of a blob storage file if it exits on the specified path.
        /// </summary>
        /// <param name="resourceId">The resourceId</param> 
        /// <param name="version">The blob storage version</param>
        /// <returns></returns>
        Task<Response> DeletePolicyVersionAsync(string resourceId, string version);

        /// <summary>
        /// Tries to acquire a blob lease on the base blob for the provided filepath.
        /// </summary>
        /// <param name="resourceId">The resourceId</param> 
        /// <returns>The LeaseId if a release was possible, otherwise null</returns>
        Task<string> TryAcquireBlobLease(string resourceId);

        /// <summary>
        /// Releases a blob lease on the base blob for the resource policy for the provided resource id using the provided leaseId.
        /// </summary>
        /// <param name="resourceId">The resourceId</param> 
        /// <param name="leaseId">The lease id from to release</param>
        void ReleaseBlobLease(string resourceId, string leaseId);

        /// <summary>
        /// Checks whether there exists a blob for the specific resource
        /// </summary>
        /// <param name="resourceId">The resourceId</param> 
        /// <returns>Bool whether the blob exists or not</returns>
        Task<bool> PolicyExistsAsync(string resourceId);
    }
}
