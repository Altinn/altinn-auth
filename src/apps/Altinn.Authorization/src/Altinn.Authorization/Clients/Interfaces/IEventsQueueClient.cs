using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Models.EventLog;

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
        /// <param name="authorizationEvent">the authorization event to be sent to queue</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Returns a queue receipt</returns>
        public Task<QueuePostReceipt> EnqueueAuthorizationEvent(AuthorizationEvent authorizationEvent, CancellationToken cancellationToken = default);
    }
}
