using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.AccessMgmt.Persistence.Services.Models
{
    /// <summary>
    /// Represents the result of an attempt to add a party, including the unique identifier of the party and a flag
    /// indicating if the party was created or already existed.
    /// </summary>
    public class AddPartyResult
    {
        public Guid PartyUuid { get; set; }

        public bool PartyCreated { get; set; }
    }
}
