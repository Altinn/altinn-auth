using Altinn.ResourceRegistry.Core.Enums;
using System.Text.Json.Serialization;

namespace Altinn.ResourceRegistry.Core.Models
{
    public class ResourceReference
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ReferenceSource? ReferenceSource { get; set; }

        public string? Reference { get; set; }


        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ReferenceType? ReferenceType { get; set; }
    }
}
