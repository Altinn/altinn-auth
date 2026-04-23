using System.Globalization;
using Altinn.Urn;

namespace Altinn.Authorization.Api.Contracts.AccessManagement
{
    /// <summary>
    /// A unique reference to an access package in the form of an URN.
    /// </summary>
    [KeyValueUrn]
    public abstract partial record AccessPackageUrn
    {
        /// <summary>
        /// Try to get the urn as an access package identifier.
        /// </summary>
        /// <param name="packageId">The resulting access package identifier.</param>
        /// <returns><see langword="true"/> if this is an access package URN, otherwise <see langword="false"/>.</returns>
        [UrnKey("altinn:accesspackage", Canonical = true)]
        public partial bool IsAccessPackage(out AccessPackageIdentifier packageId);
    }
}
