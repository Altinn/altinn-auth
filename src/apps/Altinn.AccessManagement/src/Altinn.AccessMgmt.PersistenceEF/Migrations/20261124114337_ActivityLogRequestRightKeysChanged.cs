using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <summary>
    /// Logs a change of the requested right keys on a resource request as <c>RightsChanged</c>.
    /// The update trigger on <c>dbo.requestassignmentresource</c> gets a new <c>WHEN</c> condition,
    /// and its function is replaced through the AlterTable annotation operation handled by
    /// <c>CustomMigrationsSqlGenerator</c>.
    /// </summary>
    /// <remarks>
    /// The trigger scripts create a trigger only if it does not exist, so the old trigger is
    /// dropped first; otherwise it would keep its old condition and never fire on a key change.
    /// </remarks>
    public partial class ActivityLogRequestRightKeysChanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS activitylog_requestassignmentresource_update_trg ON dbo.requestassignmentresource;");

            migrationBuilder.AlterTable(name: "requestassignmentresource", schema: "dbo")
                .Annotation(ActivityLogExtensions.AnnotationName, ActivityLogEFConfiguration.Version);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores the status-only condition. The function still handles key changes,
            // but with this condition it only ever writes Updated rows, as before.
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS activitylog_requestassignmentresource_update_trg ON dbo.requestassignmentresource;
                CREATE TRIGGER activitylog_requestassignmentresource_update_trg AFTER UPDATE ON dbo.requestassignmentresource
                FOR EACH ROW WHEN (OLD.status IS DISTINCT FROM NEW.status) EXECUTE FUNCTION dbo.activitylog_requestassignmentresource_update_fn();
                """);
        }
    }
}
