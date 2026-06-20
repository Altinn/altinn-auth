using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Tests.Unit.Validation;

/// <summary>
/// Direct unit tests for the pure-logic branches of <see cref="ConnectionWriteValidation.ValidateWriteOpInput"/>:
/// From/To existence and the entity-type allow-list enforcement driven by <see cref="ConnectionOptions"/>.
/// </summary>
[UnitTest]
public class ConnectionWriteValidationTest
{
    private static Entity OrgEntity() => new() { Id = Guid.NewGuid(), TypeId = EntityTypeConstants.Organization };

    private static Entity PersonEntity() => new() { Id = Guid.NewGuid(), TypeId = EntityTypeConstants.Person };

    private static ConnectionOptions Options(Action<ConnectionOptions> configure) => new(configure);

    [Fact]
    public void ValidateWriteOpInput_FromMissing_ReturnsProblem()
    {
        var problem = ConnectionWriteValidation.ValidateWriteOpInput(null, OrgEntity(), Options(_ => { }));
        problem.Should().NotBeNull();
    }

    [Fact]
    public void ValidateWriteOpInput_ToMissing_ReturnsProblem()
    {
        var problem = ConnectionWriteValidation.ValidateWriteOpInput(OrgEntity(), null, Options(_ => { }));
        problem.Should().NotBeNull();
    }

    [Fact]
    public void ValidateWriteOpInput_NoTypeConstraints_BothExist_ReturnsNull()
    {
        var problem = ConnectionWriteValidation.ValidateWriteOpInput(OrgEntity(), PersonEntity(), Options(_ => { }));
        problem.Should().BeNull();
    }

    [Fact]
    public void ValidateWriteOpInput_BothTypeConstraintsSatisfied_ReturnsNull()
    {
        var problem = ConnectionWriteValidation.ValidateWriteOpInput(
            OrgEntity(),
            PersonEntity(),
            Options(o =>
            {
                o.AllowedWriteFromEntityTypes = [EntityTypeConstants.Organization];
                o.AllowedWriteToEntityTypes = [EntityTypeConstants.Person];
            }));
        problem.Should().BeNull();
    }

    [Fact]
    public void ValidateWriteOpInput_FromTypeNotAllowed_ReturnsProblem()
    {
        var problem = ConnectionWriteValidation.ValidateWriteOpInput(
            PersonEntity(),
            PersonEntity(),
            Options(o =>
            {
                o.AllowedWriteFromEntityTypes = [EntityTypeConstants.Organization];
                o.AllowedWriteToEntityTypes = [EntityTypeConstants.Person];
            }));
        problem.Should().NotBeNull();
    }

    [Fact]
    public void ValidateWriteOpInput_FromOnlyConstraint_NotAllowed_ReturnsProblem()
    {
        var problem = ConnectionWriteValidation.ValidateWriteOpInput(
            PersonEntity(),
            PersonEntity(),
            Options(o => o.AllowedWriteFromEntityTypes = [EntityTypeConstants.Organization]));
        problem.Should().NotBeNull();
    }

    [Fact]
    public void ValidateWriteOpInput_ToOnlyConstraint_NotAllowed_ReturnsProblem()
    {
        var problem = ConnectionWriteValidation.ValidateWriteOpInput(
            OrgEntity(),
            OrgEntity(),
            Options(o => o.AllowedWriteToEntityTypes = [EntityTypeConstants.Person]));
        problem.Should().NotBeNull();
    }

    [Fact]
    public void ValidateWriteOpInput_ToOnlyConstraint_Allowed_ReturnsNull()
    {
        var problem = ConnectionWriteValidation.ValidateWriteOpInput(
            OrgEntity(),
            PersonEntity(),
            Options(o => o.AllowedWriteToEntityTypes = [EntityTypeConstants.Person]));
        problem.Should().BeNull();
    }
}
