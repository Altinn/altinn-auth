using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Cli.ServiceBus.MassTransit;

[ExcludeFromCodeCoverage]
public static class FakeMassTransitEnvelope
{
    public static FakeMassTransitEnvelope<T> Create<T>(T message)
        where T : IFakeMassTransitMessage<T>
        => new(message);
}

[ExcludeFromCodeCoverage]
public sealed record FakeMassTransitEnvelope<T>
    where T : IFakeMassTransitMessage<T>
{
    public FakeMassTransitEnvelope(T message)
    {
        Message = message;
    }

    [JsonPropertyName("messageId")]
    public Guid MessageId => T.MessageId(Message);

    [JsonPropertyName("correlationId")]
    public Guid CorrelationId => T.CorrelationId(Message);

    [JsonPropertyName("messageType")]
    public IEnumerable<Utf8String> MessageType => [T.MessageUrn];

    [JsonPropertyName("message")]
    public T Message { get; }
}

[ExcludeFromCodeCoverage]
public sealed record AnonymousFakeMessageEnvelope
{
    [JsonPropertyName("messageId")]
    public required JsonElement MessageId { get; init; }

    [JsonPropertyName("messageType")]
    public required JsonElement MessageType { get; init; }

    [JsonPropertyName("message")]
    public required JsonElement Message { get; init; }
}

public interface IFakeMassTransitMessage<T>
    where T : IFakeMassTransitMessage<T>
{
    public static abstract Utf8String MessageUrn { get; }

    public static abstract Guid MessageId(T message);

    public static abstract Guid CorrelationId(T message);
}

[ExcludeFromCodeCoverage]
[JsonConverter(typeof(Utf8String.JsonConverter))]
public readonly struct Utf8String
{
    private readonly ReadOnlyMemory<byte> _value;

    private Utf8String(ReadOnlyMemory<byte> value)
    {
        _value = value;
    }

    public static implicit operator Utf8String(ReadOnlySpan<byte> value)
        => new(value.ToArray());

    public static implicit operator Utf8String(ReadOnlyMemory<byte> value)
        => new(value);

    public static implicit operator ReadOnlySpan<byte>(Utf8String value)
        => value._value.Span;

    private sealed class JsonConverter
        : JsonConverter<Utf8String>
    {
        public override Utf8String Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, Utf8String value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value._value.Span);
        }
    }
}
