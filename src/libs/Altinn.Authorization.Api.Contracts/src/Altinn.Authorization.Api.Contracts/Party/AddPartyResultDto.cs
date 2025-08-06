using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.Authorization.Api.Contracts.Party
{
    public class AddPartyResultDto
    {
        public Guid PartyUuid { get; set; }

        public bool PartyCreated { get; set; }
    }
}
