using System;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <summary>
    /// Installs the activity log: the range-partitioned <c>dbo.activitylog</c> table, its support
    /// functions (embedded <c>ActivityLogFunctions.sql</c>), the initial partitions and backfill
    /// seed (embedded <c>ActivityLogPartitions.sql</c>), and — through the AlterTable annotation
    /// operations handled by <c>CustomMigrationsSqlGenerator</c> — the triggers on the assignment,
    /// delegation and request tables that write the log. The AlterTable operations are hand-written
    /// because the EF model differ does not scaffold operations for custom annotation changes.
    /// </summary>
    /// <inheritdoc />
    public partial class ActivityLog : Migration
    {
        private const string FunctionsScriptResource =
            "Altinn.AccessMgmt.PersistenceEF.Migrations.ActivityLogFunctions.sql";

        private const string PartitionsScriptResource =
            "Altinn.AccessMgmt.PersistenceEF.Migrations.ActivityLogPartitions.sql";

        private static readonly string[] ActivityLogTables =
        [
            "assignment",
            "assignmentpackage",
            "assignmentresource",
            "assignmentinstance",
            "delegation",
            "delegationpackage",
            "delegationresource",
            "requestassignment",
            "requestassignmentpackage",
            "requestassignmentresource",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The support functions must exist before the table (dbo.uuid_generate_v7 is its id default).
            migrationBuilder.Sql(ReadScript(FunctionsScriptResource));

            migrationBuilder.CreateTable(
                name: "activitylog",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "dbo.uuid_generate_v7()"),
                    when = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    subtype = table.Column<int>(type: "integer", nullable: true),
                    trigger = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: true),
                    byid = table.Column<Guid>(type: "uuid", nullable: true),
                    byname = table.Column<string>(type: "text", nullable: true),
                    sourceid = table.Column<Guid>(type: "uuid", nullable: true),
                    operationid = table.Column<string>(type: "text", nullable: true),
                    fromid = table.Column<Guid>(type: "uuid", nullable: true),
                    fromname = table.Column<string>(type: "text", nullable: true),
                    fromtype = table.Column<string>(type: "text", nullable: true),
                    toid = table.Column<Guid>(type: "uuid", nullable: true),
                    toname = table.Column<string>(type: "text", nullable: true),
                    totype = table.Column<string>(type: "text", nullable: true),
                    viaid = table.Column<Guid>(type: "uuid", nullable: true),
                    vianame = table.Column<string>(type: "text", nullable: true),
                    viatype = table.Column<string>(type: "text", nullable: true),
                    roleid = table.Column<Guid>(type: "uuid", nullable: true),
                    rolename = table.Column<string>(type: "text", nullable: true),
                    viaroleid = table.Column<Guid>(type: "uuid", nullable: true),
                    viarolename = table.Column<string>(type: "text", nullable: true),
                    packageid = table.Column<Guid>(type: "uuid", nullable: true),
                    packagename = table.Column<string>(type: "text", nullable: true),
                    resourceid = table.Column<Guid>(type: "uuid", nullable: true),
                    resourcename = table.Column<string>(type: "text", nullable: true),
                    instanceid = table.Column<string>(type: "text", nullable: true),
                    itemid = table.Column<Guid>(type: "uuid", nullable: false),
                    parentid = table.Column<Guid>(type: "uuid", nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activitylog", x => new { x.when, x.id });
                });

            migrationBuilder.CreateTable(
                name: "activitylogbackfillprogress",
                schema: "dbo",
                columns: table => new
                {
                    source = table.Column<string>(type: "text", nullable: false),
                    cutoff = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cursor = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activitylogbackfillprogress", x => x.source);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activitylog_byid_when",
                schema: "dbo",
                table: "activitylog",
                columns: new[] { "byid", "when" });

            migrationBuilder.CreateIndex(
                name: "ix_activitylog_fromid_when",
                schema: "dbo",
                table: "activitylog",
                columns: new[] { "fromid", "when" });

            migrationBuilder.CreateIndex(
                name: "ix_activitylog_itemid",
                schema: "dbo",
                table: "activitylog",
                column: "itemid");

            migrationBuilder.CreateIndex(
                name: "ix_activitylog_parentid",
                schema: "dbo",
                table: "activitylog",
                column: "parentid");

            migrationBuilder.CreateIndex(
                name: "ix_activitylog_toid_when",
                schema: "dbo",
                table: "activitylog",
                columns: new[] { "toid", "when" });

            migrationBuilder.CreateIndex(
                name: "ix_activitylog_viaid_when",
                schema: "dbo",
                table: "activitylog",
                columns: new[] { "viaid", "when" });

            foreach (var table in ActivityLogTables)
            {
                migrationBuilder.AlterTable(name: table, schema: "dbo")
                    .Annotation(ActivityLogExtensions.AnnotationName, ActivityLogEFConfiguration.Version);
            }

            migrationBuilder.Sql(ReadScript(PartitionsScriptResource));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS activitylog_assignment_insert_trg ON dbo.assignment;
                DROP TRIGGER IF EXISTS activitylog_assignment_delete_trg ON dbo.assignment;
                DROP TRIGGER IF EXISTS activitylog_assignmentpackage_insert_trg ON dbo.assignmentpackage;
                DROP TRIGGER IF EXISTS activitylog_assignmentpackage_delete_trg ON dbo.assignmentpackage;
                DROP TRIGGER IF EXISTS activitylog_assignmentresource_insert_trg ON dbo.assignmentresource;
                DROP TRIGGER IF EXISTS activitylog_assignmentresource_delete_trg ON dbo.assignmentresource;
                DROP TRIGGER IF EXISTS activitylog_assignmentinstance_insert_trg ON dbo.assignmentinstance;
                DROP TRIGGER IF EXISTS activitylog_assignmentinstance_update_trg ON dbo.assignmentinstance;
                DROP TRIGGER IF EXISTS activitylog_assignmentinstance_delete_trg ON dbo.assignmentinstance;
                DROP TRIGGER IF EXISTS activitylog_delegation_insert_trg ON dbo.delegation;
                DROP TRIGGER IF EXISTS activitylog_delegation_delete_trg ON dbo.delegation;
                DROP TRIGGER IF EXISTS activitylog_delegationpackage_insert_trg ON dbo.delegationpackage;
                DROP TRIGGER IF EXISTS activitylog_delegationpackage_delete_trg ON dbo.delegationpackage;
                DROP TRIGGER IF EXISTS activitylog_delegationresource_insert_trg ON dbo.delegationresource;
                DROP TRIGGER IF EXISTS activitylog_delegationresource_delete_trg ON dbo.delegationresource;
                DROP TRIGGER IF EXISTS activitylog_requestassignment_insert_trg ON dbo.requestassignment;
                DROP TRIGGER IF EXISTS activitylog_requestassignment_delete_trg ON dbo.requestassignment;
                DROP TRIGGER IF EXISTS activitylog_requestassignmentpackage_insert_trg ON dbo.requestassignmentpackage;
                DROP TRIGGER IF EXISTS activitylog_requestassignmentpackage_update_trg ON dbo.requestassignmentpackage;
                DROP TRIGGER IF EXISTS activitylog_requestassignmentpackage_delete_trg ON dbo.requestassignmentpackage;
                DROP TRIGGER IF EXISTS activitylog_requestassignmentresource_insert_trg ON dbo.requestassignmentresource;
                DROP TRIGGER IF EXISTS activitylog_requestassignmentresource_update_trg ON dbo.requestassignmentresource;
                DROP TRIGGER IF EXISTS activitylog_requestassignmentresource_delete_trg ON dbo.requestassignmentresource;

                DROP FUNCTION IF EXISTS dbo.activitylog_assignment_insert_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_assignment_delete_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_assignmentpackage_insert_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_assignmentpackage_delete_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_assignmentresource_insert_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_assignmentresource_delete_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_assignmentinstance_insert_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_assignmentinstance_update_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_assignmentinstance_delete_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_delegation_insert_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_delegation_delete_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_delegationpackage_insert_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_delegationpackage_delete_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_delegationresource_insert_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_delegationresource_delete_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_requestassignment_insert_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_requestassignment_delete_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_requestassignmentpackage_insert_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_requestassignmentpackage_update_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_requestassignmentpackage_delete_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_requestassignmentresource_insert_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_requestassignmentresource_update_fn();
                DROP FUNCTION IF EXISTS dbo.activitylog_requestassignmentresource_delete_fn();

                DROP FUNCTION IF EXISTS dbo.activitylog_ensure_month_partitions(int);
                DROP FUNCTION IF EXISTS dbo.activitylog_ensure_partitions(date, date);
                DROP FUNCTION IF EXISTS dbo.activitylog_requestassignment_info(uuid);
                DROP FUNCTION IF EXISTS dbo.activitylog_delegation_info(uuid);
                DROP FUNCTION IF EXISTS dbo.activitylog_assignment_info(uuid);
                DROP FUNCTION IF EXISTS dbo.activitylog_resource_name(uuid);
                DROP FUNCTION IF EXISTS dbo.activitylog_package_name(uuid);
                DROP FUNCTION IF EXISTS dbo.activitylog_role_name(uuid);
                DROP FUNCTION IF EXISTS dbo.activitylog_entity_name(uuid);
                DROP FUNCTION IF EXISTS dbo.activitylog_entity_info(uuid);
                """);

            migrationBuilder.DropTable(
                name: "activitylog",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "activitylogbackfillprogress",
                schema: "dbo");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS dbo.uuid_generate_v7();");
        }

        private static string ReadScript(string resourceName)
        {
            var assembly = typeof(ActivityLog).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded migration script '{resourceName}' was not found in assembly '{assembly.FullName}'.");
            using var reader = new System.IO.StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
