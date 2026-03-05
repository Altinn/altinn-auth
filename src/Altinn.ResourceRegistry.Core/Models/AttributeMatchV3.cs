using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Altinn.Urn;

namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Model describing a pair of AttributeId and AttributeValue for use in matching in XACML policies, for instance a resource, a user, a party or an action. This model also implements equality comparers to allow for easy comparison of attribute matches.
    /// </summary>
    public class AttributeMatchV3 : IEqualityComparer<AttributeMatchV3>
    {
        /// <summary>
        /// ctor
        /// </summary>
        public AttributeMatchV3()
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="id">type</param>
        /// <param name="value">value</param>
        public AttributeMatchV3(string id, object value)
        {
            Type = id;
            Value = value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the attribute id for the match
        /// </summary>
        [Required]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the attribute value for the match
        /// </summary>
        [Required]
        public string Value { get; set; }

        /// <inheritdoc/>
        public bool Equals(AttributeMatchV3 x, AttributeMatchV3 y) => x.Equals(y);

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as AttributeMatchV3);

        private bool Equals(AttributeMatchV3 other) => Type.Equals(other?.Type, StringComparison.InvariantCultureIgnoreCase) && Value == other?.Value;

        /// <inheritdoc/>
        public int GetHashCode([DisallowNull] AttributeMatchV3 obj) => obj.GetHashCode();

        /// <inheritdoc/>
        public override int GetHashCode() => (Type, Value).GetHashCode();

        /// <summary>
        /// String representation of the attribute
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>
            $"{Type.ToLowerInvariant()}:{Value}";

        /// <summary>
        /// Creates a KeyValueUrn from the attribute match
        /// </summary>
        /// <returns>KeyValueUrn</returns>
        public KeyValueUrn ToKeyValueUrn() =>
            KeyValueUrn.CreateUnchecked($"{Type.ToLowerInvariant()}:{Value}", Type.Length + 1);
    }
}
