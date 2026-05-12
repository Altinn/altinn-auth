namespace Altinn.Authorization.Api.Contracts.Authorization
{
    /// <summary>
    /// Represents a data transfer object for the response from the Policy Information Point (PIP).
    /// </summary>
    public class PipResponseDto
    {
        /// <summary>
        /// Role access info
        /// </summary>
        public List<RoleUrn> Roles { get; set; } = [];

        /// <summary>
        /// Access package info.
        /// </summary>
        public List<AccessPackageUrn> AccessPackages { get; set; } = [];
    }
}
