using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement;

public class DelegationBatchInputDto
{
    public List<Permission> Values { get; set; } = [];

    public class Permission
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }
        
        [JsonPropertyName("package")]
        public List<string> Packages { get; set; }
    }
}
