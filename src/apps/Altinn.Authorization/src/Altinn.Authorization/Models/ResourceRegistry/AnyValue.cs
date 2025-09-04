using System.Diagnostics.CodeAnalysis;
using Altinn.Urn;

namespace Altinn.Authorization.Models.ResourceRegistry
{
    [KeyValueUrn]
    public abstract partial record AccessListUrn
    {
        [UrnKey("altinn:access-list")]
        public partial bool IsAccessList(out AnyValue value);
    }

    public record AnyValue(string Value)
        : IParsable<AnyValue>
        , ISpanParsable<AnyValue>
        , IFormattable
        , ISpanFormattable
    {
        public static AnyValue Parse(string s, IFormatProvider? provider)
        {
            return new(s);
        }

        public static AnyValue Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return new(new string(s));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out AnyValue result)
        {
            if (s is null)
            {
                result = null;
                return false;
            }

            result = new(s);
            return true;
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out AnyValue result)
        {
            result = new(new string(s));
            return true;
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return Value;
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            if (Value.AsSpan().TryCopyTo(destination))
            {
                charsWritten = Value.Length;
                return true;
            }

            charsWritten = 0;
            return false;
        }
    }
}
