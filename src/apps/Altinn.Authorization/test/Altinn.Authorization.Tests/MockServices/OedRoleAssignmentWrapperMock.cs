using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Models.Oed;
using Altinn.Platform.Authorization.Services.Interface;

namespace Altinn.Platform.Authorization.IntegrationTests.MockServices
{
    public class OedRoleAssignmentWrapperMock : IOedRoleAssignmentWrapper
    {
        public Task<List<OedRoleAssignment>> GetOedRoleAssignments(string from, string to)
        {
            List<OedRoleAssignment> oedRoleAssignments = new();
            if (from == "11895696716" && to == "13923949741")
            {
                OedRoleAssignment oedRoleAssignment = new OedRoleAssignment
                {
                    Created = DateTime.Now,
                    From = from,
                    To = to,
                    OedRoleCode = "urn:digitaltdodsbo:formuesfullmakt"
                };
                oedRoleAssignments.Add(oedRoleAssignment);
            }

            return Task.FromResult(oedRoleAssignments);
        }
    }
}
