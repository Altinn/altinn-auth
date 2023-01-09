using Altinn.ResourceRegistry.Core;
using Azure;
using Azure.Storage.Blobs.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ResourceRegistryTest.Mocks
{
    public class PolicyRepositoryMock : IPolicyRepository
    {
        public Task<Response> DeletePolicyVersionAsync(string filepath, string version)
        {
            throw new NotImplementedException();
        }

        public async Task<Stream> GetPolicyAsync(string resourceId)
        {
            resourceId = Path.Combine(GetPolicyContainerPath(), resourceId, "resourcepolicy.xml");
            if (File.Exists(resourceId))
            {
                return new FileStream(resourceId, FileMode.Open, FileAccess.Read, FileShare.Read); 
            }

            return null;
        }

        public Task<Stream> GetPolicyVersionAsync(string filepath, string version)
        {
            throw new NotImplementedException();
        }

        public Task<bool> PolicyExistsAsync(string filepath)
        {
            throw new NotImplementedException();
        }

        public void ReleaseBlobLease(string filepath, string leaseId)
        {
            throw new NotImplementedException();
        }

        public Task<string> TryAcquireBlobLease(string filepath)
        {
            throw new NotImplementedException();
        }

        public Task<Response<BlobContentInfo>> WritePolicyAsync(string filepath, Stream fileStream)
        {
            BlobContentInfo mockedBlobInfo = BlobsModelFactory.BlobContentInfo(new ETag("ETagSuccess"), DateTime.Now, new byte[1], DateTime.Now.ToUniversalTime().ToString(), "encryptionKeySha256", "encryptionScope", 1);
            Mock<Response<BlobContentInfo>> mockResponse = new Mock<Response<BlobContentInfo>>();
            mockResponse.SetupGet(r => r.Value).Returns(mockedBlobInfo);

            Mock<Response> responseMock = new Mock<Response>();
            responseMock.SetupGet(r => r.Status).Returns((int)HttpStatusCode.Created);
            mockResponse.Setup(r => r.GetRawResponse()).Returns(responseMock.Object);

            return Task.FromResult(mockResponse.Object);
        }

        public Task<Response<BlobContentInfo>> WritePolicyConditionallyAsync(string filepath, Stream fileStream, string blobLeaseId)
        {
            throw new NotImplementedException();
        }

        private static string GetPolicyContainerPath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRepositoryMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "ResourcePolicies");
        }

    }
}
