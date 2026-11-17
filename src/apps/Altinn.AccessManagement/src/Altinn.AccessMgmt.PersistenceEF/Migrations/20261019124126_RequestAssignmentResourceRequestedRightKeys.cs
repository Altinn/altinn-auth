using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class RequestAssignmentResourceRequestedRightKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "requestedrightkeys",
                schema: "dbo",
                table: "requestassignmentresource",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "requestedrightkeys",
                schema: "dbo_history",
                table: "auditrequestassignmentresource",
                type: "text[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "requestedrightkeys",
                schema: "dbo",
                table: "requestassignmentresource");

            migrationBuilder.DropColumn(
                name: "requestedrightkeys",
                schema: "dbo_history",
                table: "auditrequestassignmentresource");
        }
    }
}
