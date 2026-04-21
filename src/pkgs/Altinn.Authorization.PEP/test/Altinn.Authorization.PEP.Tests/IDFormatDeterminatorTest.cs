using Altinn.Common.PEP.Models;
using Altinn.Common.PEP.Utils;

using Xunit;

namespace Altinn.Authorization.PEP.Tests
{
    public class IDFormatDeterminatorTest
    {
        [Fact]
        public void DetermineIDFormat_Null_ReturnsUnknown()
        {
            Assert.Equal(IDFormat.Unknown, IDFormatDeterminator.DetermineIDFormat(null));
        }

        [Fact]
        public void DetermineIDFormat_EmptyString_ReturnsUnknown()
        {
            Assert.Equal(IDFormat.Unknown, IDFormatDeterminator.DetermineIDFormat(string.Empty));
        }

        [Fact]
        public void DetermineIDFormat_ValidOrgNumber_ReturnsOrgNr()
        {
            // 991825827 is used in other tests as valid org number
            Assert.Equal(IDFormat.OrgNr, IDFormatDeterminator.DetermineIDFormat("991825827"));
        }

        [Fact]
        public void DetermineIDFormat_ValidSSN_ReturnsSSN()
        {
            // 01014922047 is used in other tests as valid SSN
            Assert.Equal(IDFormat.SSN, IDFormatDeterminator.DetermineIDFormat("01014922047"));
        }

        [Fact]
        public void DetermineIDFormat_ValidUsername_ReturnsUserName()
        {
            Assert.Equal(IDFormat.UserName, IDFormatDeterminator.DetermineIDFormat("ola.nordmann"));
        }

        [Fact]
        public void DetermineIDFormat_ValidUsernameWithAt_ReturnsUserName()
        {
            Assert.Equal(IDFormat.UserName, IDFormatDeterminator.DetermineIDFormat("user@example.com"));
        }

        [Fact]
        public void DetermineIDFormat_WhitespaceOnly_ReturnsUnknown()
        {
            Assert.Equal(IDFormat.Unknown, IDFormatDeterminator.DetermineIDFormat("   "));
        }

        [Theory]
        [InlineData("991825827", true)]
        [InlineData("123456789", false)]
        [InlineData("12345678", false)]
        [InlineData("1234567890", false)]
        [InlineData("abcdefghi", false)]
        [InlineData("99182582a", false)]
        public void IsValidOrganizationNumber_VariousInputs_ReturnsExpected(string input, bool expected)
        {
            Assert.Equal(expected, IDFormatDeterminator.IsValidOrganizationNumber(input));
        }

        [Theory]
        [InlineData("01014922047", true)]
        [InlineData("12345678901", false)]
        [InlineData("1234567890", false)]
        [InlineData("abcdefghijk", false)]
        [InlineData("0101492204a", false)]
        public void IsValidSSN_VariousInputs_ReturnsExpected(string input, bool expected)
        {
            Assert.Equal(expected, IDFormatDeterminator.IsValidSSN(input));
        }

        [Theory]
        [InlineData("ola.nordmann", true)]
        [InlineData("user@example.com", true)]
        [InlineData("user-name", true)]
        [InlineData("user_name", true)]
        [InlineData("12345", false)]
        [InlineData("", false)]
        public void IsValidUserName_VariousInputs_ReturnsExpected(string input, bool expected)
        {
            Assert.Equal(expected, IDFormatDeterminator.IsValidUserName(input));
        }
    }
}
