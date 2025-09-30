#nullable enable

using System.ComponentModel.DataAnnotations;

namespace Altinn.Authorization.Api.Contracts.Party
{
    public class PartyBaseDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the party.
        /// </summary>
        [Required]
        public Guid PartyUuid { get; set; }

        /// <summary>
        /// Gets or sets the type of the PArty Uuid.
        /// </summary>
        [Required]
        public required string EntityType { get; set; }

        /// <summary>
        /// Gets or sets the type of the entity variant.
        /// </summary>
        [Required]
        public required string EntityVariantType { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [Required]
        public required string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who created the entity.
        /// </summary>
        public Guid? CreatedBy { get; set; }
    }
}
