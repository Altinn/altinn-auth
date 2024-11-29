using System;
using System.Text.Json.Serialization;

namespace Altinn.Platform.Authorization.Models.Oed
{
    /// <summary>
    /// Model for OED role assignment
    /// </summary>
    public class OedRoleAssignment
    {
        /// <summary>
        /// The OED/Digitalt dødsbo role code 
        /// </summary>
        [JsonPropertyName("urn:digitaltdodsbo:rolecode")]
        public string OedRoleCode { get; set; }

        /// <summary>
        /// The deceased person's pid
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// The inheriting person's pid
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// The datetime created
        /// </summary>
        public DateTime Created { get; set; }
    }
}