using System.ComponentModel.DataAnnotations;
using Altinn.Authorization.Api.Contracts.Consent;

namespace Altinn.Authorization.Api.Contracts.AccessManagement
{
    /// <summary>
    /// Defines  package delegation between two 
    /// </summary>
    public class ServiceOwnerAccessPackageDelegation
    {
        public required ServiceOwnerConnectionPartyUrn From { get; set; }

        public required ServiceOwnerConnectionPartyUrn To { get; set; }

        public required AccessPackageUrn PackageUrn { get; set; }
    }
}
