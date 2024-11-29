using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Profile.Models;

namespace Altinn.Platform.Authorization.Services.Interface
{
    /// <summary>
    /// Interface for actions related to profile
    /// </summary>
    public interface IProfile
    {
        /// <summary>
        /// Method that fetches the user profile for a given user id
        /// </summary>
        /// <param name="userId">The user id</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>The user profile</returns>
        Task<UserProfile> GetUserProfile(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Method that fetches the user profile for a given Norwegian social security number
        /// </summary>
        /// <param name="personId">The 11 digit "social security number" of the person</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        Task<UserProfile> GetUserProfileByPersonId(string personId, CancellationToken cancellationToken = default);
    }
}
