using System.Buffers.Text;

using Altinn.Authorization.Cli.Utils;

namespace Altinn.Authorization.Cli.Tests.Utils;

[UnitTest]
public class PasswordHashTest
{
    [Fact]
    public void Validate_HashFromSameCredentials_ReturnsTrue()
    {
        string hash = PasswordHash.Create("client-a", "s3cret");

        PasswordHash.Validate("client-a", "s3cret", hash).Should().BeTrue();
    }

    [Fact]
    public void Validate_WrongPassword_ReturnsFalse()
    {
        string hash = PasswordHash.Create("client-a", "s3cret");

        PasswordHash.Validate("client-a", "wrong", hash).Should().BeFalse();
    }

    [Fact]
    public void Validate_WrongUserName_ReturnsFalse()
    {
        // The username is part of the derived-key input, so a different user
        // must not validate against the same hash.
        string hash = PasswordHash.Create("client-a", "s3cret");

        PasswordHash.Validate("client-b", "s3cret", hash).Should().BeFalse();
    }

    [Fact]
    public void Create_CalledTwiceForSameCredentials_ProducesDifferentHashesThatBothValidate()
    {
        // A fresh random salt per call means identical credentials never produce
        // identical output, yet both outputs must validate.
        string first = PasswordHash.Create("client-a", "s3cret");
        string second = PasswordHash.Create("client-a", "s3cret");

        first.Should().NotBe(second);
        PasswordHash.Validate("client-a", "s3cret", first).Should().BeTrue();
        PasswordHash.Validate("client-a", "s3cret", second).Should().BeTrue();
    }

    [Fact]
    public void Validate_UnknownVersionByte_ThrowsFormatException()
    {
        string hash = PasswordHash.Create("client-a", "s3cret");
        byte[] raw = Base64Url.DecodeFromChars(hash);
        raw[0] = 0x02; // only version 1 is defined
        string tampered = Base64Url.EncodeToString(raw);

        Action act = () => PasswordHash.Validate("client-a", "s3cret", tampered);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Validate_BodyOfWrongLength_ThrowsFormatException()
    {
        // Valid version byte, but the salt+key body must be 48 bytes.
        byte[] raw = [0x01, 0x00, 0x00, 0x00];
        string malformed = Base64Url.EncodeToString(raw);

        Action act = () => PasswordHash.Validate("client-a", "s3cret", malformed);

        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("", "p", "hash")]
    [InlineData("u", "", "hash")]
    [InlineData("u", "p", "")]
    public void Validate_BlankArgument_Throws(string user, string password, string hash)
    {
        Action act = () => PasswordHash.Validate(user, password, hash);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_BlankArgument_Throws()
    {
        ((Action)(() => PasswordHash.Create("", "p"))).Should().Throw<ArgumentException>();
        ((Action)(() => PasswordHash.Create("u", ""))).Should().Throw<ArgumentException>();
    }

    // Characterization test, not an endorsement. The derived key is computed over
    // the string $"{userName}:{password}", so the ':' separator is ambiguous:
    // distinct credential pairs that share the same "user:pass" concatenation
    // collide. A hash issued for ("client", "a:b") also validates ("client:a", "b").
    // This pins the current (weak) behavior so that fixing it - length-prefixing
    // the username, or a version-2 format - has to flip this assertion on purpose.
    // Tracked as a finding on #3382.
    [Fact]
    public void Validate_AmbiguousUserPasswordDelimiter_CollidesAcrossCredentialPairs()
    {
        string hash = PasswordHash.Create("client", "a:b");

        PasswordHash.Validate("client:a", "b", hash).Should().BeTrue();
    }
}
