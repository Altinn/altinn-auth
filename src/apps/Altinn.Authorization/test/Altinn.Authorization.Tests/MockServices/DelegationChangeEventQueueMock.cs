using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Models.DelegationChangeEvent;
using Altinn.Platform.Authorization.Services.Implementation;
using Altinn.Platform.Authorization.Services.Interface;
using Azure.Storage.Queues.Models;

namespace Altinn.Platform.Authorization.IntegrationTests.MockServices
{
    /// <inheritdoc />
    public class DelegationChangeEventQueueMock : IDelegationChangeEventQueue
    {
        private readonly IEventMapperService _eventMapperService = new EventMapperService();

#pragma warning disable SA1401 // Fields should be private
        public List<DelegationChangeEvent> DelegationChangeEvents = new List<DelegationChangeEvent>();
#pragma warning restore SA1401 // Fields should be private

        /// <summary>
        /// Mocks pushing delegation changes to the event queue
        /// </summary>
        /// <param name="delegationChange">The delegation change stored in postgresql</param>
        public Task<SendReceipt> Push(DelegationChange delegationChange)
        {
            if (string.IsNullOrEmpty(delegationChange.AltinnAppId) || delegationChange.AltinnAppId == "error/delegationeventfail")
            {
                throw new Exception("DelegationChangeEventQueue || Push || Error");
            }

            DelegationChangeEventList dceList = _eventMapperService.MapToDelegationChangeEventList(new List<DelegationChange> { delegationChange });
            DelegationChangeEvents.AddRange(dceList.DelegationChangeEvents);

            return Task.FromResult((SendReceipt)null);
        }
    }
}
