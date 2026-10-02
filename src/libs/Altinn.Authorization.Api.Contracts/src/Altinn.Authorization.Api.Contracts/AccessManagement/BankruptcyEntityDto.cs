using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement
{
    public class BankruptcyEntityDto : CompactEntityDto
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BankruptcyEstatePermissions Permissions { get; set; }

        public BankruptcyEntityDto()
        {
        }

        public BankruptcyEntityDto(CompactEntityDto input, BankruptcyEstatePermissions permissions) 
        {
            this.Id = input.Id;
            this.Name = input.Name;
            this.Type = input.Type;
            this.Variant = input.Variant;
            this.Parent = input.Parent;
            this.Children = input.Children;
            this.PartyId = input.PartyId;
            this.UserId = input.UserId;
            this.Username = input.Username;
            this.OrganizationIdentifier = input.OrganizationIdentifier;
            this.PersonIdentifier = input.PersonIdentifier;
            this.DateOfBirth = input.DateOfBirth;
            this.DateOfDeath = input.DateOfDeath;
            this.IsDeleted = input.IsDeleted;
            this.DeletedAt = input.DeletedAt;
            this.Permissions = permissions;
        }
    }
}
