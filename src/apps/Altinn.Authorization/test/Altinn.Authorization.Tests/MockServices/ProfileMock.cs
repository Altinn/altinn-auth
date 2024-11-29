using System;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;

namespace Altinn.Platform.Authorization.IntegrationTests.MockServices
{
    public class ProfileMock : IProfile
    {
        public Task<UserProfile> GetUserProfile(int userId, CancellationToken cancellationToken = default)
        {
            UserProfile userProfile = null;
            if (userId == 20010440)
            {
                userProfile = new UserProfile { Party = new Party { SSN = "13923949741" } };
            }
            else if (userId == 13371337)
            {
                userProfile = new UserProfile { UserId = userId, Party = new Party { SSN = "13371337133" } };
            }
            else if (userId == 1337)
            {
                userProfile = new UserProfile { UserId = userId, Party = new Party { SSN = "01039012345", PartyId = 1337, PartyUuid = Guid.Parse("00000000-0000-0000-0005-000000001337"), PartyTypeName = Register.Enums.PartyType.Person } };
            }
            else if (userId == 20000517)
            {
                userProfile = new UserProfile { UserId = userId, Party = new Party { SSN = "08069402071", PartyId = 50002625, PartyUuid = Guid.Parse("00000000-0000-0000-0005-000000002625"), PartyTypeName = Register.Enums.PartyType.Person } };
            }
            else if (userId == 20000095)
            {
                userProfile = new UserProfile { UserId = userId, Party = new Party { SSN = "02056260016", PartyId = 50002203, PartyUuid = Guid.Parse("00000000-0000-0000-0005-000000002203"), PartyTypeName = Register.Enums.PartyType.Person } };
            }

            return Task.FromResult(userProfile);
        }

        public Task<UserProfile> GetUserProfileByPersonId(string personId, CancellationToken cancellationToken = default)
        {
            UserProfile userProfile = null;
            if (personId == "13923949741")
            {
                userProfile = new UserProfile { Party = new Party { SSN = personId } };
            }
            else if (personId == "13371337133")
            {
                userProfile = new UserProfile { UserId = 13371337, Party = new Party { SSN = personId } };
            }
            else if (personId == "01039012345")
            {
                userProfile = new UserProfile { UserId = 1337, Party = new Party { SSN = personId, PartyId = 1337, PartyUuid = Guid.Parse("00000000-0000-0000-0005-000000001337"), PartyTypeName = Register.Enums.PartyType.Person } };
            }
            else if (personId == "08069402071")
            {
                userProfile = new UserProfile { UserId = 20000517, Party = new Party { SSN = personId, PartyId = 50002625, PartyUuid = Guid.Parse("00000000-0000-0000-0005-000000002625"), PartyTypeName = Register.Enums.PartyType.Person } };
            }
            else if (personId == "02056260016")
            {
                userProfile = new UserProfile { UserId = 20000095, Party = new Party { SSN = personId, PartyId = 50002203, PartyUuid = Guid.Parse("00000000-0000-0000-0005-000000002203"), PartyTypeName = Register.Enums.PartyType.Person } };
            }

            return Task.FromResult(userProfile);
        }
    }
}
