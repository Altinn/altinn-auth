using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement;

public class DelegationBatchInputDto
{
    [JsonPropertyName("values")]
    public List<Permission> Values { get; set; } = [];

    public class Permission
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("packages")]
        public List<string> Packages { get; set; }
    }
}
