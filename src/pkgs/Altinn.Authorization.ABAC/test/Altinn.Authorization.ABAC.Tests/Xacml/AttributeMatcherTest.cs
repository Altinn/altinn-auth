using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;

namespace Altinn.Authorization.ABAC.Tests.Xacml;

/// <summary>
/// Direct unit tests for <see cref="AttributeMatcher"/>, the XACML attribute-match
/// function dispatch. These cover every match function and the edge cases the bundled
/// OASIS conformance suite does not exercise (substring vs membership, datatype
/// mismatch, malformed values) — until now the engine was only tested indirectly
/// through the Authorization conformance suite.
/// </summary>
[UnitTest]
public class AttributeMatcherTest
{
    private const string StringEqual = XacmlConstants.AttributeMatchFunction.StringEqual;
    private const string StringEqualIgnoreCase = XacmlConstants.AttributeMatchFunction.StringEqualIgnoreCase;
    private const string AnyUriEqual = XacmlConstants.AttributeMatchFunction.AnyUriEqual;
    private const string IntegerEqual = XacmlConstants.AttributeMatchFunction.IntegerEqual;
    private const string IntegerOneAndOnly = XacmlConstants.AttributeMatchFunction.IntegerOneAndOnly;
    private const string StringIsIn = XacmlConstants.AttributeMatchFunction.StringIsIn;
    private const string TimeEqual = XacmlConstants.AttributeMatchFunction.TimeEqual;
    private const string DateEqual = XacmlConstants.AttributeMatchFunction.DateEqual;
    private const string DateTimeEqual = XacmlConstants.AttributeMatchFunction.DateTimeEqual;
    private const string RegexpMatch = XacmlConstants.AttributeMatchFunction.RegexpMatch;

    [Theory]
    [InlineData("alice", "alice", true)]
    [InlineData("alice", "bob", false)]
    [InlineData("Alice", "alice", false)] // string-equal is case-sensitive
    public void MatchAttributes_StringEqual_ReturnsExpectedMatch(string policy, string request, bool expected)
        => AttributeMatcher.MatchAttributes(policy, request, StringEqual).Should().Be(expected);

    [Theory]
    [InlineData("Alice", "alice", true)]
    [InlineData("ALICE", "alice", true)]
    [InlineData("Alice", "alicia", false)]
    public void MatchAttributes_StringEqualIgnoreCase_ReturnsExpectedMatch(string policy, string request, bool expected)
        => AttributeMatcher.MatchAttributes(policy, request, StringEqualIgnoreCase).Should().Be(expected);

    [Theory]
    [InlineData("urn:a:b", "urn:a:b", true)]
    [InlineData("http://example.com/a", "http://example.com/b", false)]
    public void MatchAttributes_AnyUriEqual_ReturnsExpectedMatch(string policy, string request, bool expected)
        => AttributeMatcher.MatchAttributes(policy, request, AnyUriEqual).Should().Be(expected);

    [Fact]
    public void MatchAttributes_AnyUriEqual_MalformedUri_Throws()
    {
        // MatchAnyUri constructs new Uri(...) with no guard, so a non-URI value throws.
        // Pins the current fail-loud behavior.
        Action act = () => AttributeMatcher.MatchAttributes("not a uri", "also not a uri", AnyUriEqual);
        act.Should().Throw<UriFormatException>();
    }

    [Theory]
    [InlineData("5", "5", true)]
    [InlineData("5", "6", false)]
    [InlineData("007", "7", true)] // numeric equality, not string equality
    [InlineData("x", "5", false)] // non-numeric policy value -> false, not an exception
    [InlineData("5", "y", false)] // non-numeric request value -> false
    public void MatchAttributes_IntegerEqual_ReturnsExpectedMatch(string policy, string request, bool expected)
        => AttributeMatcher.MatchAttributes(policy, request, IntegerEqual).Should().Be(expected);

    [Theory]
    [InlineData("42", "42", true)]
    [InlineData("42", "43", false)]
    public void MatchAttributes_IntegerOneAndOnly_BehavesLikeIntegerEqual(string policy, string request, bool expected)
        => AttributeMatcher.MatchAttributes(policy, request, IntegerOneAndOnly).Should().Be(expected);

    // string-is-in is bag membership (per-element equality), NOT a substring test: policy value
    // "admin" must not match request value "superadmin". Regression guard for #3481 — the OASIS
    // conformance suite only uses exact-match/non-match cases, so it never caught the substring bug.
    [Theory]
    [InlineData("admin", "admin", true)]
    [InlineData("reader", "reader", true)]
    [InlineData("admin", "superadmin", false)]
    [InlineData("read", "readwrite", false)]
    public void MatchAttributes_StringIsIn_ReturnsExpectedMatch(string policy, string request, bool expected)
        => AttributeMatcher.MatchAttributes(policy, request, StringIsIn).Should().Be(expected);

    [Theory]
    [InlineData("09:30:00", "09:30:00", true)]
    [InlineData("09:30:00", "10:30:00", false)]
    [InlineData("2024-01-01T09:30:00", "2024-06-15T09:30:00", true)] // time-equal is time-of-day only; the date component must be ignored
    public void MatchAttributes_TimeEqual_ReturnsExpectedMatch(string policy, string request, bool expected)
        => AttributeMatcher.MatchAttributes(policy, request, TimeEqual).Should().Be(expected);

    [Theory]
    [InlineData("2002-09-24", "2002-09-24", true)]
    [InlineData("2002-09-24", "2002-09-25", false)]
    public void MatchAttributes_DateEqual_ReturnsExpectedMatch(string policy, string request, bool expected)
        => AttributeMatcher.MatchAttributes(policy, request, DateEqual).Should().Be(expected);

    [Theory]
    [InlineData("2002-09-24T09:30:00", "2002-09-24T09:30:00", true)]
    [InlineData("2002-09-24T09:30:00", "2002-09-24T10:30:00", false)]
    public void MatchAttributes_DateTimeEqual_ReturnsExpectedMatch(string policy, string request, bool expected)
        => AttributeMatcher.MatchAttributes(policy, request, DateTimeEqual).Should().Be(expected);

    [Theory]
    [InlineData("a.c", "abc", true)]
    [InlineData("^[0-9]+$", "12345", true)]
    [InlineData("^[0-9]+$", "12a45", false)]
    public void MatchAttributes_RegexpMatch_ReturnsExpectedMatch(string policy, string request, bool expected)
        => AttributeMatcher.MatchAttributes(policy, request, RegexpMatch).Should().Be(expected);

    [Fact]
    public void MatchAttributes_UnknownFunction_Throws()
    {
        Action act = () => AttributeMatcher.MatchAttributes("a", "a", "urn:not:a:real:function");
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void MatchAttributes_NullArgument_Throws()
    {
        Action act = () => AttributeMatcher.MatchAttributes(null!, "a", StringEqual);
        act.Should().Throw<ArgumentNullException>();
    }
}
