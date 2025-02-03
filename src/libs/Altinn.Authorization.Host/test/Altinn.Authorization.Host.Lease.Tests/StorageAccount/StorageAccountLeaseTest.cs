using Altinn.Authorization.Host.Lease.StorageAccount;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.Authorization.Host.Lease.Tests
{
    /// <summary>
    /// This class contains tests for acquiring leases on an Azure Storage Account using Altinn Lease.
    /// The tests validate the proper functionality of lease acquisition and management in a multithreaded environment.
    /// </summary>
    public class StorageAccountLeaseTest : FanoutTests
    {
        /// <summary>
        /// Gets or sets the lease instance used in the tests.
        /// </summary>
        public override IAltinnLease Lease { get; set; }

        /// <summary>
        /// Initializes the test setup by configuring the necessary services and Azure Storage Account lease.
        /// </summary>
        public StorageAccountLeaseTest()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                // builder.AddConsole();
            });

            AltinnLease.ConfigureAltinnLeaseAzureStorageAccount(services, new()
            {
                Type = AltinnLeaseType.AzureStorageAccount,
                StorageAccount = new()
                {
                    Endpoint = new Uri("https://{storage_account_name}.core.windows.net/"),
                }
            });

            Lease = services.BuildServiceProvider().GetRequiredService<IAltinnLease>() as StorageAccountLease;
        }

        /// <summary>
        /// Tests the lease acquisition with multiple threads, simulating concurrent access.
        /// This test verifies that the lease can be successfully acquired by different threads.
        /// </summary>
        /// <param name="numThreads">The number of threads to simulate for lease acquisition.</param>
        /// <returns>A task that represents the asynchronous test operation.</returns>
        [Theory(Skip = "Requires Storage Account")]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public async Task TestLease(int numThreads)
        {
            await TestThreadAquireExplosion(numThreads);
        }
    }
}
