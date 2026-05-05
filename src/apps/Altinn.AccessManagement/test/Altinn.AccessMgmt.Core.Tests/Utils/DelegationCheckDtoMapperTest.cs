using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Queries.Models;

namespace Altinn.AccessMgmt.Core.Tests.Utils;

/// <summary>
/// Pure unit tests for <see cref="DtoMapper.Convert(IEnumerable{PackageDelegationCheck})"/>.
/// </summary>
public class DelegationCheckDtoMapperTest
{
    private static readonly DtoMapper Mapper = new();

    // ── single package, single row ───────────────────────────────────────────
    [Fact]
    public void Convert_SingleRow_ReturnsSingleDto()
    {
        var packageId = Guid.NewGuid();
        var checks = new[]
        {
            MakeCheck(packageId, result: true, description: "ok")
        };

        var result = Mapper.Convert(checks).ToList();

        result.Should().HaveCount(1);
        result[0].Package.Id.Should().Be(packageId);
        result[0].Result.Should().BeTrue();
        result[0].Reasons.Should().HaveCount(1);
        result[0].Reasons.First().Description.Should().Be("ok");
    }

    // ── multiple rows for the same package are grouped ───────────────────────
    [Fact]
    public void Convert_MultipleRowsSamePackage_Grouped()
    {
        var packageId = Guid.NewGuid();
        var checks = new[]
        {
            MakeCheck(packageId, result: false, description: "reason-a"),
            MakeCheck(packageId, result: false, description: "reason-b"),
        };

        var result = Mapper.Convert(checks).ToList();

        result.Should().HaveCount(1);
        result[0].Result.Should().BeFalse();
        result[0].Reasons.Should().HaveCount(2);
    }

    [Fact]
    public void Convert_MultipleRowsSamePackage_AnyTrueYieldsTrue()
    {
        var packageId = Guid.NewGuid();
        var checks = new[]
        {
            MakeCheck(packageId, result: false, description: "denied"),
            MakeCheck(packageId, result: true, description: "allowed"),
        };

        var result = Mapper.Convert(checks).ToList();

        result.Should().HaveCount(1);
        result[0].Result.Should().BeTrue();
    }

    // ── two distinct packages produce two DTOs ───────────────────────────────
    [Fact]
    public void Convert_TwoDistinctPackages_ReturnsTwoDtos()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var checks = new[]
        {
            MakeCheck(id1, result: true, description: "r1"),
            MakeCheck(id2, result: false, description: "r2"),
        };

        var result = Mapper.Convert(checks).ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(d => d.Package.Id == id1 && d.Result);
        result.Should().Contain(d => d.Package.Id == id2 && !d.Result);
    }

    // ── reason fields are mapped ─────────────────────────────────────────────
    [Fact]
    public void Convert_ReasonFields_AreMapped()
    {
        var packageId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var check = new PackageDelegationCheck
        {
            Package = new PackageDelegationCheck.DelegationCheckPackage
            {
                Id = packageId,
                Urn = "urn:altinn:accesspackage:test",
                AreaId = Guid.NewGuid()
            },
            Result = true,
            Reason = new PackageDelegationCheck.DelegationCheckReason
            {
                Description = "desc",
                RoleId = roleId,
                RoleUrn = "urn:role",
                FromId = Guid.NewGuid(),
                FromName = "From",
                ToId = Guid.NewGuid(),
                ToName = "To"
            }
        };

        var result = Mapper.Convert([check]).ToList();

        var reason = result[0].Reasons.Single();
        reason.Description.Should().Be("desc");
        reason.RoleId.Should().Be(roleId);
        reason.RoleUrn.Should().Be("urn:role");
        reason.FromName.Should().Be("From");
        reason.ToName.Should().Be("To");
    }

    // ── helpers ──────────────────────────────────────────────────────────────
    private static PackageDelegationCheck MakeCheck(Guid packageId, bool result, string description) =>
        new()
        {
            Package = new PackageDelegationCheck.DelegationCheckPackage
            {
                Id = packageId,
                Urn = $"urn:altinn:accesspackage:{packageId}",
                AreaId = Guid.NewGuid()
            },
            Result = result,
            Reason = new PackageDelegationCheck.DelegationCheckReason
            {
                Description = description
            }
        };
}
