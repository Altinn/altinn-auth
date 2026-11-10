using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement
{
    public class BankruptcyEstateAdministratorDto
    {
        /// <summary>
        /// The entity holding the bankruptcy administration role
        /// </summary>
        [JsonPropertyName("administrator")]
        public CompactEntityDto Administrator { get; set; }

        /// <summary>
        /// The bankruptcy administrator role
        /// </summary>
        [JsonPropertyName("adminrole")]
        public CompactRoleDto AdminRole { get; set; }

        /// <summary>
        /// When the user was assigned the administrator role
        /// </summary>
        [JsonPropertyName("adminaddedat")]
        public DateTimeOffset AdminAddedAt { get; set; }

        /// <summary>
        /// Gets or sets a collection of all access information to estates for the administrator 
        /// </summary>
        [JsonPropertyName("estateaccess")]
        public List<AdminRoleAccessPackage> EstateAccess { get; set; } = [];

        /// <summary>
        /// Composite Key instances
        /// </summary>
        public class AdminRoleAccessPackage
        {
            /// <summary>
            /// The estate the admin has acces to
            /// </summary>
            [JsonPropertyName("estate")]
            public CompactEntityDto Estate { get; set; }

            /// <summary>
            /// Packages the administrator has access to for the estate
            /// </summary>
            [JsonPropertyName("packages")]
            public CompactPackageDto[] Packages { get; set; }

            /// <summary>
            /// Resources the administrator has access to for the estate
            /// </summary>
            [JsonPropertyName("resources")]
            public List<CompactResourceDto> Resources { get; set; } = [];
        }
    }
}
