using System;
using System.Collections.Generic;
using System.Text;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.Enums
{
    [Flags]
    public enum BankruptcyEstatePermissions
    {
        None = 0,
        User = 1 << 0,
        Admin = 1 << 1,

        UserAndAdmin = User | Admin
    }
}
