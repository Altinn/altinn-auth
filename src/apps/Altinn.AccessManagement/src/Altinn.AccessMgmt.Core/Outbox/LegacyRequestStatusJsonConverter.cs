using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.Core.Outbox;

/// <summary>
/// Reads <see cref="RequestStatusV2"/> in outbox messages, including the name
/// <see cref="RequestStatusV2.ApprovedWithAdjustments"/> had before it was renamed, PartialApprove.
/// A message can wait in the outbox across the deploy that renamed it. Writes the current name.
/// </summary>
internal sealed class LegacyRequestStatusJsonConverter : JsonConverter<RequestStatusV2>
{
    private static readonly JsonConverter<RequestStatusV2> Inner =
        (JsonConverter<RequestStatusV2>)new JsonStringEnumConverter().CreateConverter(typeof(RequestStatusV2), JsonSerializerOptions.Default)!;

    /// <inheritdoc/>
    public override RequestStatusV2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && reader.ValueTextEquals("PartialApprove"u8))
        {
            return RequestStatusV2.ApprovedWithAdjustments;
        }

        return Inner.Read(ref reader, typeToConvert, options);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, RequestStatusV2 value, JsonSerializerOptions options)
        => Inner.Write(writer, value, options);
}
