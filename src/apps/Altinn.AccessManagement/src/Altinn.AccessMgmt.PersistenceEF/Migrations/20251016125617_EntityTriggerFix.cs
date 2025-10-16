using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class EntityTriggerFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                create or replace function audit_entity_update_fn() returns trigger
                    language plpgsql
                as
                $$
                BEGIN
                INSERT INTO dbo_history.auditentity (
                id,name,parentid,refid,typeid,variantid,dateofbirth,deletedat,organizationidentifier,partyid,personidentifier,userid,username,
                audit_validfrom, audit_validto,
                audit_changedby, audit_changedbysystem, audit_changeoperation
                ) VALUES (
                OLD.id,OLD.name,OLD.parentid,OLD.refid,OLD.typeid,OLD.variantid,OLD.dateofbirth,OLD.deletedat,OLD.organizationidentifier,OLD.partyid,OLD.personidentifier,OLD.userid,OLD.username,
                OLD.audit_validfrom, now(),
                OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
                );
                RETURN NEW;
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                create or replace function audit_entity_update_fn() returns trigger
                    language plpgsql
                as
                $$
                BEGIN
                INSERT INTO dbo_history.auditentity (
                id,name,parentid,refid,typeid,variantid,
                audit_validfrom, audit_validto,
                audit_changedby, audit_changedbysystem, audit_changeoperation
                ) VALUES (
                OLD.id,OLD.name,OLD.parentid,OLD.refid,OLD.typeid,OLD.variantid,
                OLD.audit_validfrom, now(),
                OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
                );
                RETURN NEW;
                END;
                $$;                
                """);
        }
    }
}
