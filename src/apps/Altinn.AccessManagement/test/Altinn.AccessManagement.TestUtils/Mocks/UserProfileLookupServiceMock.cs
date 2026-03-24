using Altinn.AccessManagement.Core.Models.Profile;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Platform.Register.Models;

namespace Altinn.AccessManagement.TestUtils.Mocks;

/// <summary>
/// Mock implementation of <see cref="IUserProfileLookupService"/> that returns
/// <see cref="NewUserProfile"/> instances for known test persons based on SSN lookup.
/// </summary>
public class UserProfileLookupServiceMock : IUserProfileLookupService
{
    private static readonly Dictionary<string, Entity> _ssnToEntity = BuildSsnLookup();

    /// <inheritdoc/>
    public Task<NewUserProfile> GetUserProfile(int authnUserId, UserProfileLookup lookupIdentifier, string lastName)
    {
        string identifier = lookupIdentifier.Ssn ?? lookupIdentifier.Username;

        if (identifier is not null
            && _ssnToEntity.TryGetValue(identifier, out var entity)
            && entity.Name.Split(' ').Last().Equals(lastName, StringComparison.OrdinalIgnoreCase))
        {
            var profile = new NewUserProfile
            {
                UserId = entity.UserId ?? 0,
                UserUuid = entity.Id,
                UserName = entity.Username,
                Party = new Platform.Register.Models.Party
                {
                    PartyUuid = entity.Id,
                    Person = new Person { LastName = entity.Name.Split(' ').Last() },
                },
            };

            return Task.FromResult<NewUserProfile>(profile);
        }

        return Task.FromResult<NewUserProfile>(null);
    }

    private static Dictionary<string, Entity> BuildSsnLookup()
    {
        var lookup = new Dictionary<string, Entity>();
        AddIfPerson(lookup, TestData.MalinEmilie);
        AddIfPerson(lookup, TestData.Thea);
        AddIfPerson(lookup, TestData.JosephineYvonnesdottir);
        AddIfPerson(lookup, TestData.BodilFarmor);
        return lookup;
    }

    private static void AddIfPerson(Dictionary<string, Entity> lookup, Entity entity)
    {
        if (entity.PersonIdentifier is not null)
        {
            lookup[entity.PersonIdentifier] = entity;
        }
    }
}
