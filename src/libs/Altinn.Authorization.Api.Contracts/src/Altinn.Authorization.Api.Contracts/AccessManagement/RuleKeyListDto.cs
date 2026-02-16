using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.Authorization.Api.Contracts.AccessManagement
{
    public class RuleKeyListDto
    {
        public IEnumerable<string> RuleKeys { get; set; }
    }
}
