#nullable enable

using System.Buffers;
using System.Buffers.Text;

namespace Altinn.ResourceRegistry.Utils;

/// <summary>
/// Utility class for base 64 url encoding and decoding using the base64 web safe alphabet.
/// </summary>
internal static class Base64UrlEncoder
{
    private static readonly SearchValues<char> ReplacedValues = SearchValues.Create("-_");

    /// <summary>
    /// Returns the maximum length (in bytes) of the result if you were to decode base 64 encoded text within a char span of size "length".
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="length"/> is less than 0.
    /// </exception>
    public static int GetMaxDecodedLength(int length)
    {
        return Base64.GetMaxDecodedFromUtf8Length(length + 2);
    }

    /// <summary>
    /// Returns the maximum length (in chars) of the result if you were to encode binary data within a byte span of size "length".
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="length"/> is less than 0 or larger than 1610612733 (since encode inflates the data by 4/3).
    /// </exception>
    public static int GetMaxEncodedLength(int length)
    {
        return Base64.GetMaxEncodedToUtf8Length(length);
    }

    /// <summary>
    /// Encode binary data within a byte span into base 64 encoded text.
    /// </summary>
    /// <param name="data">The data to encode</param>
    /// <returns>The resulting base64 string</returns>
    public static string Encode(ReadOnlySpan<byte> data)
    {
        var buff = ArrayPool<char>.Shared.Rent(GetMaxEncodedLength(data.Length));
        try
        {
            Convert.TryToBase64Chars(data, buff, out var charsWritten);
            var written = buff.AsSpan(0, charsWritten).TrimEnd('=');
            written.Replace('+', '-');
            written.Replace('/', '_');
            return new string(written);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buff);
        }
    }

    /// <summary>
    /// Try to decode base 64 encoded text within a char span into a byte span.
    /// </summary>
    /// <param name="encoded">The encoded text</param>
    /// <param name="data">The span of data to write into</param>
    /// <param name="bytesWritten">The number of bytes written</param>
    /// <returns><see langword="true"/> if decoding succeeded, otherwise <see langword="false"/></returns>
    public static bool TryDecode(ReadOnlySpan<char> encoded, Span<byte> data, out int bytesWritten)
    {
        if (encoded.ContainsAny(ReplacedValues) || NeedsPadding(encoded))
        {
            return TryReplaceAndDecode(encoded, data, out bytesWritten);
        }
        else
        {
            return TryDecodeInner(encoded, data, out bytesWritten);
        }
    }

    private static bool TryReplaceAndDecode(ReadOnlySpan<char> encoded, Span<byte> data, out int bytesWritten)
    {
        var length = encoded.Length;
        var chars = ArrayPool<char>.Shared.Rent(length + 2);
        try
        {
            encoded.CopyTo(chars.AsSpan());
            chars.AsSpan(0, length).Replace('-', '+');
            chars.AsSpan(0, length).Replace('_', '/');

            encoded = PadIfNeeded(chars, length);

            return TryDecodeInner(encoded, data, out bytesWritten);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(chars);
        }
    }

    private static bool TryDecodeInner(ReadOnlySpan<char> encoded, Span<byte> data, out int bytesWritten)
    {
        return Convert.TryFromBase64Chars(encoded, data, out bytesWritten);
    }

    private static bool NeedsPadding(ReadOnlySpan<char> encoded)
    {
        return encoded.Length % 4 != 0;
    }

    private static Span<char> PadIfNeeded(Span<char> chars, int length)
    {
        switch (length % 4)
        {
            case 2:
                chars[length] = '=';
                chars[length + 1] = '=';
                return chars[..(length + 2)];
            case 3:
                chars[length] = '=';
                return chars[..(length + 1)];
            default:
                return chars[..length];
        }
    }
}
