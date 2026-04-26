using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.QueryBuilders;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.Core.Tests.PersistenceCore;

/// <summary>
/// Pure SQL-string generation tests for <see cref="PostgresQueryBuilder"/> — no database required.
/// </summary>
public class PostgresQueryBuilderTest
{
    // ── fixtures ──────────────────────────────────────────────────────────────

    private static readonly IOptions<AccessMgmtPersistenceOptions> DefaultOptions =
        Options.Create(new AccessMgmtPersistenceOptions
        {
            BaseSchema = "am",
            DbType = MgmtDbType.Postgres
        });

    private static (PostgresQueryBuilder builder, DbDefinitionRegistry registry) CreateBuilder()
    {
        var registry = new DbDefinitionRegistry(DefaultOptions);

        registry.Define<SimpleModel>(b => b
            .RegisterProperty(m => m.Id)
            .RegisterProperty(m => m.Name));

        var builder = new PostgresQueryBuilder(DefaultOptions, typeof(SimpleModel), registry);
        return (builder, registry);
    }

    private static ChangeRequestOptions AuditOptions() => new()
    {
        ChangedBy = Guid.NewGuid(),
        ChangedBySystem = Guid.NewGuid(),
        ChangeOperationId = "op-1"
    };

    // ── GetTableName ──────────────────────────────────────────────────────────

    [Fact]
    public void GetTableName_NoAlias_ContainsSchemaAndModelName()
    {
        var (builder, _) = CreateBuilder();
        var name = builder.GetTableName(includeAlias: false);
        name.Should().Contain("SimpleModel");
        name.Should().Contain("am.");
    }

    [Fact]
    public void GetTableName_WithAlias_ContainsAsClause()
    {
        var (builder, _) = CreateBuilder();
        var name = builder.GetTableName(includeAlias: true);
        name.Should().Contain("AS SimpleModel");
    }

    // ── BuildInsertQuery ──────────────────────────────────────────────────────

    [Fact]
    public void BuildInsertQuery_ContainsInsertKeyword()
    {
        var (builder, _) = CreateBuilder();
        var parameters = new List<GenericParameter>
        {
            new("Id", Guid.NewGuid()),
            new("Name", "Test")
        };

        var sql = builder.BuildInsertQuery(parameters, AuditOptions());
        sql.Should().Contain("INSERT INTO");
    }

    [Fact]
    public void BuildInsertQuery_ContainsAllParameterNames()
    {
        var (builder, _) = CreateBuilder();
        var parameters = new List<GenericParameter>
        {
            new("Id", Guid.NewGuid()),
            new("Name", "Test")
        };

        var sql = builder.BuildInsertQuery(parameters, AuditOptions());
        sql.Should().Contain("@Id").And.Contain("@Name");
    }

    [Fact]
    public void BuildInsertQuery_ContainsTableName()
    {
        var (builder, _) = CreateBuilder();
        var parameters = new List<GenericParameter> { new("Id", Guid.NewGuid()) };
        var sql = builder.BuildInsertQuery(parameters, AuditOptions());
        sql.Should().Contain("SimpleModel");
    }

    // ── BuildUpdateQuery ──────────────────────────────────────────────────────

    [Fact]
    public void BuildUpdateQuery_ContainsUpdateKeyword()
    {
        var (builder, _) = CreateBuilder();
        var parameters = new List<GenericParameter> { new("Name", "Updated") };

        var sql = builder.BuildUpdateQuery(parameters, AuditOptions());
        sql.Should().Contain("UPDATE");
    }

    [Fact]
    public void BuildUpdateQuery_ContainsWhereId()
    {
        var (builder, _) = CreateBuilder();
        var parameters = new List<GenericParameter> { new("Name", "Updated") };

        var sql = builder.BuildUpdateQuery(parameters, AuditOptions());
        sql.Should().Contain("WHERE id = @_id");
    }

    [Fact]
    public void BuildUpdateQuery_ContainsSetClause()
    {
        var (builder, _) = CreateBuilder();
        var parameters = new List<GenericParameter> { new("Name", "Updated") };

        var sql = builder.BuildUpdateQuery(parameters, AuditOptions());
        sql.Should().Contain("Name = @Name");
    }

    // ── BuildSingleNullUpdateQuery ────────────────────────────────────────────

    [Fact]
    public void BuildSingleNullUpdateQuery_ContainsSetNull()
    {
        var (builder, _) = CreateBuilder();
        var parameter = new GenericParameter("Name", null);

        var sql = builder.BuildSingleNullUpdateQuery(parameter, AuditOptions());
        sql.Should().Contain("Name = NULL");
    }

    [Fact]
    public void BuildSingleNullUpdateQuery_ContainsWhereId()
    {
        var (builder, _) = CreateBuilder();
        var parameter = new GenericParameter("Name", null);

        var sql = builder.BuildSingleNullUpdateQuery(parameter, AuditOptions());
        sql.Should().Contain("WHERE id = @_id");
    }

    // ── BuildBasicSelectQuery ─────────────────────────────────────────────────

    [Fact]
    public void BuildBasicSelectQuery_ContainsSelectFrom()
    {
        var (builder, _) = CreateBuilder();
        var sql = builder.BuildBasicSelectQuery(new RequestOptions(), Enumerable.Empty<GenericFilter>());
        sql.Should().Contain("SELECT").And.Contain("FROM");
    }

    // ── model ─────────────────────────────────────────────────────────────────

    private class SimpleModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
