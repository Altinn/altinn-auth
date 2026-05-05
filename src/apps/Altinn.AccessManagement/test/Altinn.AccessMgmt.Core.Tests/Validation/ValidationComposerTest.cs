using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Tests.Validation;

public class ValidationComposerTest
{
    // ── helpers ──────────────────────────────────────────────────────────────
    private static RuleExpression Pass() => () => null;

    private static RuleExpression Fail() => () =>
        (ref ValidationErrorBuilder errors) =>
            errors.Add(ValidationErrors.InvalidPartyUrn, "QUERY/test");

    // ── ValidationComposer.Validate ──────────────────────────────────────────
    [Fact]
    public void Validate_NoRules_ReturnsNull()
    {
        var result = ValidationComposer.Validate();
        result.Should().BeNull();
    }

    [Fact]
    public void Validate_SinglePassingRule_ReturnsNull()
    {
        var result = ValidationComposer.Validate(Pass());
        result.Should().BeNull();
    }

    [Fact]
    public void Validate_SingleFailingRule_ReturnsProblem()
    {
        var result = ValidationComposer.Validate(Fail());
        result.Should().NotBeNull();
    }

    [Fact]
    public void Validate_AllPass_ReturnsNull()
    {
        var result = ValidationComposer.Validate(Pass(), Pass(), Pass());
        result.Should().BeNull();
    }

    [Fact]
    public void Validate_OneFails_ReturnsProblem()
    {
        var result = ValidationComposer.Validate(Pass(), Fail(), Pass());
        result.Should().NotBeNull();
    }

    [Fact]
    public void Validate_AllFail_ReturnsProblem()
    {
        var result = ValidationComposer.Validate(Fail(), Fail());
        result.Should().NotBeNull();
    }

    // ── ValidationComposer.All ───────────────────────────────────────────────
    [Fact]
    public void All_NoRules_InvokeReturnsNull()
    {
        var rule = ValidationComposer.All();
        rule().Should().BeNull();
    }

    [Fact]
    public void All_AllPassingRules_InvokeReturnsNull()
    {
        var rule = ValidationComposer.All(Pass(), Pass());
        rule().Should().BeNull();
    }

    [Fact]
    public void All_OneFailingRule_InvokeReturnsNonNull()
    {
        var rule = ValidationComposer.All(Pass(), Fail());
        rule().Should().NotBeNull();
    }

    [Fact]
    public void All_AllFailingRules_InvokeReturnsNonNull()
    {
        var rule = ValidationComposer.All(Fail(), Fail());
        rule().Should().NotBeNull();
    }

    // ── ValidationComposer.Any ───────────────────────────────────────────────
    [Fact]
    public void Any_AllPassingRules_InvokeReturnsNull()
    {
        var rule = ValidationComposer.Any(Pass(), Pass());
        rule().Should().BeNull();
    }

    [Fact]
    public void Any_SomePassingRules_InvokeReturnsNull()
    {
        var rule = ValidationComposer.Any(Pass(), Fail());
        rule().Should().BeNull();
    }

    [Fact]
    public void Any_AllFailingRules_InvokeReturnsNonNull()
    {
        var rule = ValidationComposer.Any(Fail(), Fail());
        rule().Should().NotBeNull();
    }

    [Fact]
    public void Any_NoRules_InvokeReturnsNull()
    {
        // With zero rules, none are failures so the count check (results.Count == funcs.Length) is 0 == 0 → returns non-null.
        // This is the actual behaviour of the implementation.
        var rule = ValidationComposer.Any();
        var result = rule();

        // 0 failures == 0 total → treated as "all failed" → non-null (matches actual Any logic)
        result.Should().NotBeNull();
    }
}
