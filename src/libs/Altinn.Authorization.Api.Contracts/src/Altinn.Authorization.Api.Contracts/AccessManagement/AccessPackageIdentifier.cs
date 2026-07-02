using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Api.Contracts.AccessManagement
{
    public sealed record AccessPackageIdentifier(string Value)
: IFormattable
, IParsable<AccessPackageIdentifier>
        , ISpanParsable<AccessPackageIdentifier>
    {
        public static AccessPackageIdentifier Parse(string s, IFormatProvider? provider)
            => new(s);

        public static AccessPackageIdentifier Parse(ReadOnlySpan<char> s, IFormatProvider provider)
            => new(new string(s));

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out AccessPackageIdentifier result)
        {
            result = new(s);
            return true;
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, [MaybeNullWhen(false)] out AccessPackageIdentifier result)
        {
            result = new(new string(s));
            return true;
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
            => Value;
    }
}
