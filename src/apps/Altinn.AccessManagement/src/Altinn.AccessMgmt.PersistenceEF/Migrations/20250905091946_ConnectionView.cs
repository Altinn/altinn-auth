using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class ConnectionView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE VIEW dbo.connection AS
                SELECT a.fromid,
                       a.roleid,
                       NULL::uuid     AS viaid,
                       NULL::uuid     AS viaroleid,
                       a.toid,
                       ap.packageid,
                       NULL::uuid     AS resourceid,
                       'Direct'::text AS reason
                FROM dbo.assignment a
                         JOIN dbo.assignmentpackage ap ON ap.assignmentid = a.id
                UNION ALL
                SELECT a.fromid,
                       a.roleid,
                       NULL::uuid     AS viaid,
                       NULL::uuid     AS viaroleid,
                       a.toid,
                       rp.packageid,
                       NULL::uuid     AS resourceid,
                       'Direct'::text AS reason
                FROM dbo.assignment a
                         JOIN dbo.rolepackage rp ON rp.roleid = a.roleid and rp.hasaccess = true
                UNION ALL
                SELECT a.fromid,
                       a.roleid,
                       NULL::uuid     AS viaid,
                       NULL::uuid     AS viaroleid,
                       a.toid,
                       rp.packageid,
                       NULL::uuid     AS resourceid,
                       'Direct'::text AS reason
                FROM dbo.assignment a
                         JOIN dbo.rolemap rm ON a.roleid = rm.hasroleid
                         JOIN dbo.rolepackage rp ON rp.roleid = rm.getroleid AND rp.hasaccess = true
                UNION ALL
                SELECT a.fromid,
                       a.roleid,
                       a.toid          AS viaid,
                       a2.roleid       AS viaroleid,
                       a2.toid,
                       ap.packageid,
                       NULL::uuid      AS resourceid,
                       'KeyRole'::text AS reason
                FROM dbo.assignment a
                         JOIN dbo.assignment a2 ON a.toid = a2.fromid
                         JOIN dbo.role r ON a2.roleid = r.id AND r.iskeyrole = true
                         JOIN dbo.assignmentpackage ap ON ap.assignmentid = a.id
                UNION ALL
                SELECT a.fromid,
                       a.roleid,
                       a.toid          AS viaid,
                       a2.roleid       AS viaroleid,
                       a2.toid,
                       rp.packageid,
                       NULL::uuid      AS resourceid,
                       'KeyRole'::text AS reason
                FROM dbo.assignment a
                         JOIN dbo.assignment a2 ON a.toid = a2.fromid
                         JOIN dbo.role r ON a2.roleid = r.id AND r.iskeyrole = true
                         JOIN dbo.rolepackage rp ON rp.roleid = a.roleid AND rp.hasaccess = true
                UNION ALL
                SELECT fa.fromid,
                       fa.roleid,
                       fa.toid            AS viaid,
                       ta.roleid          AS viaroleid,
                       ta.toid,
                       dp.packageid,
                       NULL::uuid         AS resourceid,
                       'Delegation'::text AS reason
                FROM dbo.delegation d
                         JOIN dbo.assignment fa ON fa.id = d.fromid
                         JOIN dbo.assignment ta ON ta.id = d.toid
                         JOIN dbo.delegationpackage dp ON dp.delegationid = d.id
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW dbo.connection;");
        }
    }
}
