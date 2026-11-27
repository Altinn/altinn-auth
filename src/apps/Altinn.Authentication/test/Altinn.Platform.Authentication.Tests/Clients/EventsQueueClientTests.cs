using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Altinn.Platform.Authentication.Clients;
using Altinn.Platform.Authentication.Configuration;
using Altinn.Platform.Authentication.Model;
using Microsoft.Extensions.Options;
using Xunit;

namespace Altinn.Platform.Authentication.Tests.Clients
{
    public class EventsQueueClientTests
    {
        [Fact]
        public async Task EnqueueAuthenticationEvent_QueueUnreachable_FailsFastAndRetriesOnNextEvent()
        {
            // Arrange: a loopback port nothing listens on, so every connection is refused.
            EventsQueueClient client = CreateClient("http://127.0.0.1:1/devstoreaccount1");
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Act
            QueuePostReceipt first = await client.EnqueueAuthenticationEvent("{}");
            QueuePostReceipt second = await client.EnqueueAuthenticationEvent("{}");
            stopwatch.Stop();

            // Assert: both fail with the cause, and the bounded retry policy keeps it to seconds,
            // where the SDK default holds each call for minutes.
            Assert.False(first.Success);
            Assert.NotNull(first.Exception);
            Assert.False(second.Success);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), $"Two failed enqueues took {stopwatch.Elapsed}.");
        }

        [Fact]
        public async Task EnqueueAuthenticationEvent_QueueNeverAnswers_GivesUpAtTheWriteDeadline()
        {
            // Arrange: a listener that accepts connections and never answers, so each attempt waits
            // for its network timeout and only the overall write deadline ends the call.
            using TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            ConcurrentBag<TcpClient> heldOpen = [];
            _ = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        heldOpen.Add(await listener.AcceptTcpClientAsync());
                    }
                }
                catch (Exception)
                {
                    // The listener is stopped when the test ends.
                }
            });

            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            EventsQueueClient client = CreateClient($"http://127.0.0.1:{port}/devstoreaccount1");
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Act
            QueuePostReceipt receipt = await client.EnqueueAuthenticationEvent("{}");
            stopwatch.Stop();

            // Assert: the write fails within the 10-second deadline (plus scheduling slack).
            Assert.False(receipt.Success);
            Assert.NotNull(receipt.Exception);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(15), $"The write took {stopwatch.Elapsed}.");

            listener.Stop();
            foreach (TcpClient c in heldOpen)
            {
                c.Dispose();
            }
        }

        private static EventsQueueClient CreateClient(string queueEndpoint) => new(Options.Create(new QueueStorageSettings
        {
            // Azurite's published development account; only the endpoint matters here.
            EventLogConnectionString =
                "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;" +
                "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
                $"QueueEndpoint={queueEndpoint}",
            AuthenticationEventQueueName = "authenticationeventqueue",
        }));
    }
}
