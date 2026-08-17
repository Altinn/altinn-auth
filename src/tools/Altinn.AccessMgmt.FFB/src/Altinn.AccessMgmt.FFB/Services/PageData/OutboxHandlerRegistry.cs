namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// Static metadata for the known outbox handlers. This is the single place to register
/// a new handler: add its name to <see cref="KnownHandlers"/> and, when applicable, its
/// filterable JSON fields to <see cref="FilterFields"/> and its example payload to
/// <see cref="Schemas"/>.
/// </summary>
public static class OutboxHandlerRegistry
{
    /// <summary>Handler names selectable in the outbox filter.</summary>
    public static IReadOnlyList<string> KnownHandlers { get; } =
    [
        "access_added", "access_removed",
        "agent_added", "agent_removed",
        "client_added", "client_removed",
        "rightholder_added", "rightholder_removed",
        "instance_added", "instance_removed",
        "request_reviewed", "request_pending",
    ];

    /// <summary>Per-handler filterable fields: UI label and the JSON key matched in the message data.</summary>
    public static IReadOnlyDictionary<string, (string Label, string JsonKey)[]> FilterFields { get; } = new Dictionary<string, (string Label, string JsonKey)[]>
    {
        ["access_added"] = [("Fra (FromId)", "FromId"), ("Til (ToId)", "ToId")],
        ["access_removed"] = [("Fra (FromId)", "FromId"), ("Til (ToId)", "ToId")],
        ["agent_added"] = [("Agent (AgentId)", "AgentId"), ("Provider (ProviderId)", "ProviderId")],
        ["agent_removed"] = [("Agent (AgentId)", "AgentId"), ("Provider (ProviderId)", "ProviderId")],
        ["client_added"] = [("Agent (AgentId)", "AgentId"), ("Provider (ProviderId)", "ProviderId")],
        ["client_removed"] = [("Agent (AgentId)", "AgentId"), ("Provider (ProviderId)", "ProviderId")],
        ["rightholder_added"] = [("Fra (FromId)", "FromId"), ("Til (ToId)", "ToId")],
        ["rightholder_removed"] = [("Fra (FromId)", "FromId"), ("Til (ToId)", "ToId")],
        ["instance_added"] = [("Fra (FromId)", "FromId"), ("Til (ToId)", "ToId")],
        ["instance_removed"] = [("Fra (FromId)", "FromId"), ("Til (ToId)", "ToId")],
        ["request_reviewed"] = [("Reviewer", "ReviewerId"), ("Mottaker (RecipientId)", "RecipientId")],
        ["request_pending"] = [("Requester (RequesterId)", "RequesterId"), ("Mottaker (RecipientId)", "RecipientId")],
    };

    /// <summary>Per-handler example JSON payload shown as documentation in the filter panel.</summary>
    public static IReadOnlyDictionary<string, string> Schemas { get; } = new Dictionary<string, string>
    {
        ["access_added"] = "{\n  \"FromId\": \"<guid>\",\n  \"ToId\": \"<guid>\",\n  \"PackageIds\": [\"<guid>\"],\n  \"ResourceIds\": [\"<guid>\"],\n  \"Updated\": 1\n}",
        ["access_removed"] = "{\n  \"FromId\": \"<guid>\",\n  \"ToId\": \"<guid>\",\n  \"PackageIds\": [\"<guid>\"],\n  \"ResourceIds\": [\"<guid>\"],\n  \"Updated\": 1\n}",
        ["agent_added"] = "{\n  \"AgentId\": \"<guid>\",\n  \"ProviderId\": \"<guid>\"\n}",
        ["agent_removed"] = "{\n  \"AgentId\": \"<guid>\",\n  \"ProviderId\": \"<guid>\"\n}",
        ["client_added"] = "{\n  \"AgentId\": \"<guid>\",\n  \"ProviderId\": \"<guid>\",\n  \"Clients\": [\"<guid>\"],\n  \"Updated\": 1\n}",
        ["client_removed"] = "{\n  \"AgentId\": \"<guid>\",\n  \"ProviderId\": \"<guid>\",\n  \"Clients\": [\"<guid>\"],\n  \"Updated\": 1\n}",
        ["rightholder_added"] = "{\n  \"FromId\": \"<guid>\",\n  \"ToId\": \"<guid>\"\n}",
        ["rightholder_removed"] = "{\n  \"FromId\": \"<guid>\",\n  \"ToId\": \"<guid>\"\n}",
        ["instance_added"] = "{\n  \"FromId\": \"<guid>\",\n  \"ToId\": \"<guid>\",\n  \"Instances\": [{\"InstanceIds\": [\"<string>\"], \"ResourceId\": \"<guid>\"}],\n  \"Updated\": 1\n}",
        ["instance_removed"] = "{\n  \"FromId\": \"<guid>\",\n  \"ToId\": \"<guid>\",\n  \"Instances\": [{\"InstanceIds\": [\"<string>\"], \"ResourceId\": \"<guid>\"}],\n  \"Updated\": 1\n}",
        ["request_reviewed"] = "{\n  \"ReviewerId\": \"<guid>\",\n  \"RecipientId\": \"<guid>\",\n  \"Resources\": [{\"Ref\": \"<guid>\", \"IsApproved\": true}],\n  \"Packages\": [{\"Ref\": \"<guid>\", \"IsApproved\": true}],\n  \"Updated\": 1\n}",
        ["request_pending"] = "{\n  \"RequesterId\": \"<guid>\",\n  \"RecipientId\": \"<guid>\",\n  \"ResourceIds\": [\"<guid>\"],\n  \"PackageIds\": [\"<guid>\"],\n  \"Updated\": 1\n}",
    };
}
