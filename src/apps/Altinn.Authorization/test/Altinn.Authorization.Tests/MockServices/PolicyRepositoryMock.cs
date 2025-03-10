using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Altinn.Platform.Authorization.Repositories.Interface;
using Azure;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Altinn.Platform.Authorization.IntegrationTests.MockServices
{
    public class PolicyRepositoryMock : IPolicyRepository
    {
        private readonly ILogger<PolicyRepositoryMock> _logger;

        public PolicyRepositoryMock(ILogger<PolicyRepositoryMock> logger)
        {
            _logger = logger;
        }

        public Task<Stream> GetPolicyAsync(string filepath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetTestDataStream(filepath));
        }

        public Task<Stream> GetPolicyVersionAsync(string filepath, string version, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetTestDataStream(filepath));
        }

        public Task<Response<BlobContentInfo>> WritePolicyAsync(string filepath, Stream fileStream, CancellationToken cancellationToken = default)
        {
            return WriteStreamToTestDataFolder(filepath, fileStream);
        }

        public Task<Response> DeletePolicyVersionAsync(string filepath, string version, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<BlobContentInfo>> WritePolicyConditionallyAsync(string filepath, Stream fileStream, string blobLeaseId, CancellationToken cancellationToken = default)
        {
            if (blobLeaseId == "CorrectLeaseId" && !filepath.Contains("error/blobstorageleaselockwritefail"))
            {
                return await WriteStreamToTestDataFolder(filepath, fileStream);
            }

            throw new RequestFailedException((int)HttpStatusCode.PreconditionFailed, "The condition specified using HTTP conditional header(s) is not met.");
        }

        public Task<string> TryAcquireBlobLease(string filepath, CancellationToken cancellationToken = default)
        {
            if (filepath.Contains("error/blobstoragegetleaselockfail"))
            {
                return Task.FromResult((string)null);
            }

            return Task.FromResult("CorrectLeaseId");
        }

        public Task ReleaseBlobLease(string filepath, string leaseId)
        {
            return Task.CompletedTask;
        }

        public Task<bool> PolicyExistsAsync(string filepath, CancellationToken cancellationToken = default)
        {
            string fullpath = Path.Combine(GetDataInputBlobPath(), filepath);

            if (File.Exists(fullpath))
            {
                return Task.FromResult(true);
            }

            _logger.LogWarning("Policy not found for full path" + fullpath);

            return Task.FromResult(false);
        }

        private static string GetDataOutputBlobPath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRepositoryMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "blobs", "output");
        }

        private static string GetDataInputBlobPath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRepositoryMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "blobs", "input");
        }

        private static Stream GetTestDataStream(string filepath)
        {
            string dataPath = Path.Combine(GetDataInputBlobPath(), filepath);
            Stream ms = new MemoryStream();
            if (File.Exists(dataPath))
            {
                using FileStream file = new FileStream(dataPath, FileMode.Open, FileAccess.Read);
                file.CopyTo(ms);
            }

            return ms;
        }

        private static async Task<Response<BlobContentInfo>> WriteStreamToTestDataFolder(string filepath, Stream fileStream)
        {
            string dataPath = Path.Combine(GetDataOutputBlobPath(), filepath);

            if (!Directory.Exists(Path.GetDirectoryName(dataPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dataPath));
            }

            int filesize;

            using (Stream streamToWriteTo = File.Open(dataPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                await fileStream.CopyToAsync(streamToWriteTo);
                streamToWriteTo.Flush();
                filesize = (int)streamToWriteTo.Length;
            }

            BlobContentInfo mockedBlobInfo = BlobsModelFactory.BlobContentInfo(new ETag("ETagSuccess"), DateTime.Now, new byte[1], DateTime.Now.ToUniversalTime().ToString(), "encryptionKeySha256", "encryptionScope", 1);
            Mock<Response<BlobContentInfo>> mockResponse = new Mock<Response<BlobContentInfo>>();
            mockResponse.SetupGet(r => r.Value).Returns(mockedBlobInfo);

            Mock<Response> responseMock = new Mock<Response>();
            responseMock.SetupGet(r => r.Status).Returns((int)HttpStatusCode.Created);
            mockResponse.Setup(r => r.GetRawResponse()).Returns(responseMock.Object);

            return mockResponse.Object;
        }
    }
}
