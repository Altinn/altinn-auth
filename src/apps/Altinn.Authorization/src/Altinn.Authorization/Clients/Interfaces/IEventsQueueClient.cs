using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Models;

namespace Altinn.Platform.Authorization.Clients.Interfaces
{
    /// <summary>
    /// Describes the necessary methods for an implementation of an events queue client.
    /// </summary>
    public interface IEventsQueueClient
    {
        /// <summary>
        /// Enqueues the provided content to the Event Log queue
        /// </summary>
        /// <param name="content">The content to push to the queue in string format</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Returns a queue receipt</returns>
        public Task<QueuePostReceipt> EnqueueAuthorizationEvent(string content, CancellationToken cancellationToken = default);
    }
}
