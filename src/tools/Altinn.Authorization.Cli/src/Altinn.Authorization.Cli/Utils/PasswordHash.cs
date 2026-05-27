using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Nerdbank.Streams;

namespace Altinn.Authorization.Cli.Utils;

/// <summary>
/// Provides functionality to create/validate identity hashes.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class PasswordHash
{
    // Secure RNG
    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

    /// <summary>
    /// Creates a new identity hash string for the given username and password.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The password.</param>
    /// <returns>The identity hash string.</returns>
    public static string Create(string userName, string password)
    {
        Guard.IsNotNullOrWhiteSpace(userName);
        Guard.IsNotNullOrWhiteSpace(password);

        using var seq = new Sequence<byte>(ArrayPool<byte>.Shared);
        seq.Write((ReadOnlySpan<byte>)[01]); // version
        V1.Create(seq, userName, password);

        return BufferExtensions.Base64UrlEncodeToString(seq.AsReadOnlySequence);
    }

    /// <summary>
    /// Validates the provided client identity string against the given username and password.
    /// </summary>
    /// <param name="userName">The provided userName.</param>
    /// <param name="password">The provided password.</param>
    /// <param name="clientHash">The client hash string to validate against.</param>
    /// <returns><see langword="true"/> if the client hash matches the provided username and password; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="FormatException">Thrown when the client hash is not in a valid format.</exception>
    /// <exception cref="ArgumentException">Thrown when any of the arguments are invalid.</exception>
    public static bool Validate(string userName, string password, string clientHash)
    {
        Guard.IsNotNullOrWhiteSpace(userName);
        Guard.IsNotNullOrWhiteSpace(password);
        Guard.IsNotNullOrWhiteSpace(clientHash);

        var maxDecodedLength = Base64Url.GetMaxDecodedLength(clientHash.Length);
        var buffer = ArrayPool<byte>.Shared.Rent(maxDecodedLength);
        try
        {
            var written = Base64Url.DecodeFromChars(clientHash, buffer);
            var version = buffer[0];
            var data = buffer.AsSpan(1, written - 1);

            return version switch
            {
                01 => V1.Validate(data, userName, password),
                _ => ThrowHelper.ThrowFormatException<bool>("Invalid client hash version"),
            };
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    private static class V1
    {
        private const int IterCount = 1_000_000;
        private const int SaltLength = 128 / 8; // 128 bits
        private const int DerivedKeyLength = 256 / 8; // 256 bits
        private const int MaxUserNameLength = 256; // arbitrary limit to prevent abuse
        private static readonly KeyDerivationPrf _prf = KeyDerivationPrf.HMACSHA512;

        public static void Create(IBufferWriter<byte> writer, string userName, string password)
        {
            if (userName.Length > MaxUserNameLength)
            {
                ThrowHelper.ThrowArgumentException(nameof(userName), "The username is too long");
            }

            byte[] salt = new byte[SaltLength];
            _rng.GetBytes(salt);

            var derivedKey = DeriveKey(userName, password, salt);
            writer.Write(salt);
            writer.Write(derivedKey);
        }

        public static bool Validate(ReadOnlySpan<byte> data, string userName, string password)
        {
            if (userName.Length > MaxUserNameLength)
            {
                ThrowHelper.ThrowArgumentException(nameof(userName), "The username is too long");
            }

            if (data.Length != SaltLength + DerivedKeyLength)
            {
                ThrowHelper.ThrowFormatException<bool>("Invalid hash length");
            }

            byte[] salt = new byte[SaltLength];
            data[..SaltLength].CopyTo(salt);

            var derivedKey = data.Slice(SaltLength, DerivedKeyLength);
            var expectedKey = DeriveKey(userName, password, salt);
            return CryptographicOperations.FixedTimeEquals(derivedKey, expectedKey);
        }

        private static byte[] DeriveKey(string userName, string password, byte[] salt)
            => KeyDerivation.Pbkdf2($"{userName}:{password}", salt, _prf, IterCount, DerivedKeyLength);
    }
}
