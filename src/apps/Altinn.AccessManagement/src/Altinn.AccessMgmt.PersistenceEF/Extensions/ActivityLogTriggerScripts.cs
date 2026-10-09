using Altinn.AccessMgmt.PersistenceEF.Utils;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

/// <summary>
/// Hand-written trigger SQL for the tables that feed <c>dbo.activitylog</c>, emitted by
/// <see cref="CustomMigrationsSqlGenerator"/> for entities marked with
/// <see cref="ActivityLogExtensions.EnableActivityLog"/>.
/// </summary>
/// <remarks>
/// Contracts the SQL relies on, which cannot be expressed in the SQL itself:
/// <list type="bullet">
/// <item>Numeric literals map to the API contract enums and must stay in sync:
/// type 1=Assignment 2=Delegation 3=Request; subtype 1=Package 2=Resource 3=Instance;
/// trigger 1=Created 2=Updated 3=Deleted; status = <c>RequestStatus</c>.</item>
/// <item>Insert triggers read attribution from the <c>audit_*</c> columns stamped by the audit
/// BEFORE-trigger; update triggers read the <c>app.*</c> GUCs directly (raw-SQL writers do not
/// refresh the audit columns on UPDATE); delete triggers read the <c>session_audit_context</c>
/// temp table, exactly like the audit delete triggers.</item>
/// <item>The support functions (<c>dbo.uuid_generate_v7</c>, <c>dbo.activitylog_*_info</c>/<c>_name</c>)
/// are created by the embedded <c>Migrations/ActivityLogFunctions.sql</c> script, which must run
/// before any of these triggers fire.</item>
/// <item>Every function no-ops until <c>dbo.activitylog</c> exists, so replaying old migrations
/// on a fresh database cannot fail on data operations that run before the activity log migration.</item>
/// </list>
/// </remarks>
public static class ActivityLogTriggerScripts
{
    /// <summary>
    /// Returns the trigger scripts for a table, in execution order.
    /// </summary>
    public static IReadOnlyList<string> ForTable(string schema, string table)
    {
        if (!string.Equals(schema, BaseConfiguration.BaseSchema, StringComparison.Ordinal) || !Scripts.TryGetValue(table, out var scripts))
        {
            throw new InvalidOperationException(
                $"No activity log trigger scripts are registered for table '{schema}.{table}'. " +
                "Add them to ActivityLogTriggerScripts or remove EnableActivityLog() from the entity configuration.");
        }

        return scripts;
    }

    /// <summary>
    /// The tables that have registered trigger scripts.
    /// </summary>
    public static IReadOnlyCollection<string> Tables => Scripts.Keys;

    // Delete triggers are deferred constraint triggers: they run at commit, after every audit
    // trigger in the transaction has written its history row. A DB-level FK cascade deletes
    // child rows inside the parent's RI trigger, which fires before the parent's own AFTER
    // DELETE audit trigger — an immediate child trigger would then find neither a live nor a
    // history row for the parent and log NULL parties. Deferral moves the lookup past that
    // window; the session_audit_context temp table (ON COMMIT DROP) is still alive when
    // deferred triggers fire.
    private static string CreateTrigger(string table, string op, string? when = null) => $"""
        DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_{table}_{op}_trg' AND t.tgrelid = to_regclass('dbo.{table}')) THEN
        CREATE {(op == "delete" ? "CONSTRAINT " : string.Empty)}TRIGGER activitylog_{table}_{op}_trg AFTER {op.ToUpperInvariant()} ON dbo.{table}
        {(op == "delete" ? "DEFERRABLE INITIALLY DEFERRED " : string.Empty)}FOR EACH ROW {(when is null ? string.Empty : when + " ")}EXECUTE FUNCTION dbo.activitylog_{table}_{op}_fn();
        END IF; END $$;
        """;

    private static readonly Dictionary<string, IReadOnlyList<string>> Scripts = new(StringComparer.Ordinal)
    {
        ["assignment"] =
        [
            AssignmentInsertFn,
            CreateTrigger("assignment", "insert"),
            AssignmentDeleteFn,
            CreateTrigger("assignment", "delete"),
        ],
        ["assignmentpackage"] =
        [
            AssignmentPackageInsertFn,
            CreateTrigger("assignmentpackage", "insert"),
            AssignmentPackageDeleteFn,
            CreateTrigger("assignmentpackage", "delete"),
        ],
        ["assignmentresource"] =
        [
            AssignmentResourceInsertFn,
            CreateTrigger("assignmentresource", "insert"),
            AssignmentResourceDeleteFn,
            CreateTrigger("assignmentresource", "delete"),
        ],
        ["assignmentinstance"] =
        [
            AssignmentInstanceInsertFn,
            CreateTrigger("assignmentinstance", "insert"),
            AssignmentInstanceUpdateFn,
            CreateTrigger("assignmentinstance", "update", "WHEN (OLD.assignmentid IS DISTINCT FROM NEW.assignmentid)"),
            AssignmentInstanceDeleteFn,
            CreateTrigger("assignmentinstance", "delete"),
        ],
        ["delegation"] =
        [
            DelegationInsertFn,
            CreateTrigger("delegation", "insert"),
            DelegationDeleteFn,
            CreateTrigger("delegation", "delete"),
        ],
        ["delegationpackage"] =
        [
            DelegationPackageInsertFn,
            CreateTrigger("delegationpackage", "insert"),
            DelegationPackageDeleteFn,
            CreateTrigger("delegationpackage", "delete"),
        ],
        ["delegationresource"] =
        [
            DelegationResourceInsertFn,
            CreateTrigger("delegationresource", "insert"),
            DelegationResourceDeleteFn,
            CreateTrigger("delegationresource", "delete"),
        ],
        ["requestassignment"] =
        [
            RequestAssignmentInsertFn,
            CreateTrigger("requestassignment", "insert"),
            RequestAssignmentDeleteFn,
            CreateTrigger("requestassignment", "delete"),
        ],
        ["requestassignmentpackage"] =
        [
            RequestAssignmentPackageInsertFn,
            CreateTrigger("requestassignmentpackage", "insert"),
            RequestAssignmentPackageUpdateFn,
            CreateTrigger("requestassignmentpackage", "update", "WHEN (OLD.status IS DISTINCT FROM NEW.status)"),
            RequestAssignmentPackageDeleteFn,
            CreateTrigger("requestassignmentpackage", "delete"),
        ],
        ["requestassignmentresource"] =
        [
            RequestAssignmentResourceInsertFn,
            CreateTrigger("requestassignmentresource", "insert"),
            RequestAssignmentResourceUpdateFn,
            CreateTrigger("requestassignmentresource", "update", "WHEN (OLD.status IS DISTINCT FROM NEW.status)"),
            RequestAssignmentResourceDeleteFn,
            CreateTrigger("requestassignmentresource", "delete"),
        ],
    };

    private const string AssignmentInsertFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_assignment_insert_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(NEW.fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(NEW.toid);
        INSERT INTO dbo.activitylog (
        "type", "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename, itemid
        ) VALUES (
        1, 1, NEW.audit_validfrom,
        NEW.audit_changedby, dbo.activitylog_entity_name(NEW.audit_changedby), NEW.audit_changedbysystem, NEW.audit_changeoperation,
        NEW.fromid, v_from.o_name, v_from.o_type, NEW.toid, v_to.o_name, v_to.o_type,
        NEW.roleid, dbo.activitylog_role_name(NEW.roleid), NEW.id
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string AssignmentDeleteFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_assignment_delete_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        ctx RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO ctx FROM session_audit_context LIMIT 1;
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(OLD.fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(OLD.toid);
        INSERT INTO dbo.activitylog (
        "type", "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename, itemid
        ) VALUES (
        1, 3, now(),
        ctx.changed_by, dbo.activitylog_entity_name(ctx.changed_by), ctx.changed_by_system, ctx.change_operation_id,
        OLD.fromid, v_from.o_name, v_from.o_type, OLD.toid, v_to.o_name, v_to.o_type,
        OLD.roleid, dbo.activitylog_role_name(OLD.roleid), OLD.id
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string AssignmentPackageInsertFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_assignmentpackage_insert_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        v_a RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO v_a FROM dbo.activitylog_assignment_info(NEW.assignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_a.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_a.o_toid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename,
        packageid, packagename, itemid, parentid
        ) VALUES (
        1, 1, 1, NEW.audit_validfrom,
        NEW.audit_changedby, dbo.activitylog_entity_name(NEW.audit_changedby), NEW.audit_changedbysystem, NEW.audit_changeoperation,
        v_a.o_fromid, v_from.o_name, v_from.o_type, v_a.o_toid, v_to.o_name, v_to.o_type,
        v_a.o_roleid, dbo.activitylog_role_name(v_a.o_roleid),
        NEW.packageid, dbo.activitylog_package_name(NEW.packageid), NEW.id, NEW.assignmentid
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string AssignmentPackageDeleteFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_assignmentpackage_delete_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        ctx RECORD;
        v_a RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO ctx FROM session_audit_context LIMIT 1;
        SELECT * INTO v_a FROM dbo.activitylog_assignment_info(OLD.assignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_a.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_a.o_toid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename,
        packageid, packagename, itemid, parentid
        ) VALUES (
        1, 1, 3, now(),
        ctx.changed_by, dbo.activitylog_entity_name(ctx.changed_by), ctx.changed_by_system, ctx.change_operation_id,
        v_a.o_fromid, v_from.o_name, v_from.o_type, v_a.o_toid, v_to.o_name, v_to.o_type,
        v_a.o_roleid, dbo.activitylog_role_name(v_a.o_roleid),
        OLD.packageid, dbo.activitylog_package_name(OLD.packageid), OLD.id, OLD.assignmentid
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string AssignmentResourceInsertFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_assignmentresource_insert_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        v_a RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO v_a FROM dbo.activitylog_assignment_info(NEW.assignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_a.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_a.o_toid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename,
        resourceid, resourcename, itemid, parentid
        ) VALUES (
        1, 2, 1, NEW.audit_validfrom,
        NEW.audit_changedby, dbo.activitylog_entity_name(NEW.audit_changedby), NEW.audit_changedbysystem, NEW.audit_changeoperation,
        v_a.o_fromid, v_from.o_name, v_from.o_type, v_a.o_toid, v_to.o_name, v_to.o_type,
        v_a.o_roleid, dbo.activitylog_role_name(v_a.o_roleid),
        NEW.resourceid, dbo.activitylog_resource_name(NEW.resourceid), NEW.id, NEW.assignmentid
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string AssignmentResourceDeleteFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_assignmentresource_delete_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        ctx RECORD;
        v_a RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO ctx FROM session_audit_context LIMIT 1;
        SELECT * INTO v_a FROM dbo.activitylog_assignment_info(OLD.assignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_a.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_a.o_toid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename,
        resourceid, resourcename, itemid, parentid
        ) VALUES (
        1, 2, 3, now(),
        ctx.changed_by, dbo.activitylog_entity_name(ctx.changed_by), ctx.changed_by_system, ctx.change_operation_id,
        v_a.o_fromid, v_from.o_name, v_from.o_type, v_a.o_toid, v_to.o_name, v_to.o_type,
        v_a.o_roleid, dbo.activitylog_role_name(v_a.o_roleid),
        OLD.resourceid, dbo.activitylog_resource_name(OLD.resourceid), OLD.id, OLD.assignmentid
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string AssignmentInstanceInsertFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_assignmentinstance_insert_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        v_a RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO v_a FROM dbo.activitylog_assignment_info(NEW.assignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_a.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_a.o_toid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename,
        resourceid, resourcename, instanceid, itemid, parentid
        ) VALUES (
        1, 3, 1, NEW.audit_validfrom,
        NEW.audit_changedby, dbo.activitylog_entity_name(NEW.audit_changedby), NEW.audit_changedbysystem, NEW.audit_changeoperation,
        v_a.o_fromid, v_from.o_name, v_from.o_type, v_a.o_toid, v_to.o_name, v_to.o_type,
        v_a.o_roleid, dbo.activitylog_role_name(v_a.o_roleid),
        NEW.resourceid, dbo.activitylog_resource_name(NEW.resourceid), NEW.instanceid, NEW.id, NEW.assignmentid
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string AssignmentInstanceUpdateFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_assignmentinstance_update_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        v_by uuid;
        v_bysystem uuid;
        v_operation text;
        v_a RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT current_setting('app.changed_by', false) INTO v_by;
        SELECT current_setting('app.changed_by_system', false) INTO v_bysystem;
        SELECT current_setting('app.change_operation_id', false) INTO v_operation;
        SELECT * INTO v_a FROM dbo.activitylog_assignment_info(NEW.assignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_a.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_a.o_toid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename,
        resourceid, resourcename, instanceid, itemid, parentid, details
        ) VALUES (
        1, 3, 2, now(),
        v_by, dbo.activitylog_entity_name(v_by), v_bysystem, v_operation,
        v_a.o_fromid, v_from.o_name, v_from.o_type, v_a.o_toid, v_to.o_name, v_to.o_type,
        v_a.o_roleid, dbo.activitylog_role_name(v_a.o_roleid),
        NEW.resourceid, dbo.activitylog_resource_name(NEW.resourceid), NEW.instanceid, NEW.id, NEW.assignmentid,
        jsonb_build_object('previousAssignmentId', OLD.assignmentid)
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string AssignmentInstanceDeleteFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_assignmentinstance_delete_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        ctx RECORD;
        v_a RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO ctx FROM session_audit_context LIMIT 1;
        SELECT * INTO v_a FROM dbo.activitylog_assignment_info(OLD.assignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_a.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_a.o_toid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename,
        resourceid, resourcename, instanceid, itemid, parentid
        ) VALUES (
        1, 3, 3, now(),
        ctx.changed_by, dbo.activitylog_entity_name(ctx.changed_by), ctx.changed_by_system, ctx.change_operation_id,
        v_a.o_fromid, v_from.o_name, v_from.o_type, v_a.o_toid, v_to.o_name, v_to.o_type,
        v_a.o_roleid, dbo.activitylog_role_name(v_a.o_roleid),
        OLD.resourceid, dbo.activitylog_resource_name(OLD.resourceid), OLD.instanceid, OLD.id, OLD.assignmentid
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string DelegationInsertFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_delegation_insert_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        v_fa RECORD;
        v_ta RECORD;
        v_from RECORD;
        v_to RECORD;
        v_via RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO v_fa FROM dbo.activitylog_assignment_info(NEW.fromid);
        SELECT * INTO v_ta FROM dbo.activitylog_assignment_info(NEW.toid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_fa.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_ta.o_toid);
        SELECT * INTO v_via FROM dbo.activitylog_entity_info(NEW.facilitatorid);
        INSERT INTO dbo.activitylog (
        "type", "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, viaid, vianame, viatype,
        roleid, rolename, viaroleid, viarolename, itemid
        ) VALUES (
        2, 1, NEW.audit_validfrom,
        NEW.audit_changedby, dbo.activitylog_entity_name(NEW.audit_changedby), NEW.audit_changedbysystem, NEW.audit_changeoperation,
        v_fa.o_fromid, v_from.o_name, v_from.o_type, v_ta.o_toid, v_to.o_name, v_to.o_type,
        NEW.facilitatorid, v_via.o_name, v_via.o_type,
        v_fa.o_roleid, dbo.activitylog_role_name(v_fa.o_roleid),
        v_ta.o_roleid, dbo.activitylog_role_name(v_ta.o_roleid), NEW.id
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string DelegationDeleteFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_delegation_delete_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        ctx RECORD;
        v_fa RECORD;
        v_ta RECORD;
        v_from RECORD;
        v_to RECORD;
        v_via RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO ctx FROM session_audit_context LIMIT 1;
        SELECT * INTO v_fa FROM dbo.activitylog_assignment_info(OLD.fromid);
        SELECT * INTO v_ta FROM dbo.activitylog_assignment_info(OLD.toid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_fa.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_ta.o_toid);
        SELECT * INTO v_via FROM dbo.activitylog_entity_info(OLD.facilitatorid);
        INSERT INTO dbo.activitylog (
        "type", "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, viaid, vianame, viatype,
        roleid, rolename, viaroleid, viarolename, itemid
        ) VALUES (
        2, 3, now(),
        ctx.changed_by, dbo.activitylog_entity_name(ctx.changed_by), ctx.changed_by_system, ctx.change_operation_id,
        v_fa.o_fromid, v_from.o_name, v_from.o_type, v_ta.o_toid, v_to.o_name, v_to.o_type,
        OLD.facilitatorid, v_via.o_name, v_via.o_type,
        v_fa.o_roleid, dbo.activitylog_role_name(v_fa.o_roleid),
        v_ta.o_roleid, dbo.activitylog_role_name(v_ta.o_roleid), OLD.id
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string DelegationPackageInsertFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_delegationpackage_insert_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        v_d RECORD;
        v_fa RECORD;
        v_ta RECORD;
        v_from RECORD;
        v_to RECORD;
        v_via RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO v_d FROM dbo.activitylog_delegation_info(NEW.delegationid);
        SELECT * INTO v_fa FROM dbo.activitylog_assignment_info(v_d.o_fromassignmentid);
        SELECT * INTO v_ta FROM dbo.activitylog_assignment_info(v_d.o_toassignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_fa.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_ta.o_toid);
        SELECT * INTO v_via FROM dbo.activitylog_entity_info(v_d.o_facilitatorid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, viaid, vianame, viatype,
        roleid, rolename, viaroleid, viarolename, packageid, packagename, itemid, parentid, details
        ) VALUES (
        2, 1, 1, NEW.audit_validfrom,
        NEW.audit_changedby, dbo.activitylog_entity_name(NEW.audit_changedby), NEW.audit_changedbysystem, NEW.audit_changeoperation,
        v_fa.o_fromid, v_from.o_name, v_from.o_type, v_ta.o_toid, v_to.o_name, v_to.o_type,
        v_d.o_facilitatorid, v_via.o_name, v_via.o_type,
        v_fa.o_roleid, dbo.activitylog_role_name(v_fa.o_roleid),
        v_ta.o_roleid, dbo.activitylog_role_name(v_ta.o_roleid),
        NEW.packageid, dbo.activitylog_package_name(NEW.packageid), NEW.id, NEW.delegationid,
        NULLIF(jsonb_strip_nulls(jsonb_build_object('rolePackageId', NEW.rolepackageid, 'assignmentPackageId', NEW.assignmentpackageid)), '{}'::jsonb)
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string DelegationPackageDeleteFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_delegationpackage_delete_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        ctx RECORD;
        v_d RECORD;
        v_fa RECORD;
        v_ta RECORD;
        v_from RECORD;
        v_to RECORD;
        v_via RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO ctx FROM session_audit_context LIMIT 1;
        SELECT * INTO v_d FROM dbo.activitylog_delegation_info(OLD.delegationid);
        SELECT * INTO v_fa FROM dbo.activitylog_assignment_info(v_d.o_fromassignmentid);
        SELECT * INTO v_ta FROM dbo.activitylog_assignment_info(v_d.o_toassignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_fa.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_ta.o_toid);
        SELECT * INTO v_via FROM dbo.activitylog_entity_info(v_d.o_facilitatorid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, viaid, vianame, viatype,
        roleid, rolename, viaroleid, viarolename, packageid, packagename, itemid, parentid, details
        ) VALUES (
        2, 1, 3, now(),
        ctx.changed_by, dbo.activitylog_entity_name(ctx.changed_by), ctx.changed_by_system, ctx.change_operation_id,
        v_fa.o_fromid, v_from.o_name, v_from.o_type, v_ta.o_toid, v_to.o_name, v_to.o_type,
        v_d.o_facilitatorid, v_via.o_name, v_via.o_type,
        v_fa.o_roleid, dbo.activitylog_role_name(v_fa.o_roleid),
        v_ta.o_roleid, dbo.activitylog_role_name(v_ta.o_roleid),
        OLD.packageid, dbo.activitylog_package_name(OLD.packageid), OLD.id, OLD.delegationid,
        NULLIF(jsonb_strip_nulls(jsonb_build_object('rolePackageId', OLD.rolepackageid, 'assignmentPackageId', OLD.assignmentpackageid)), '{}'::jsonb)
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string DelegationResourceInsertFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_delegationresource_insert_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        v_d RECORD;
        v_fa RECORD;
        v_ta RECORD;
        v_from RECORD;
        v_to RECORD;
        v_via RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO v_d FROM dbo.activitylog_delegation_info(NEW.delegationid);
        SELECT * INTO v_fa FROM dbo.activitylog_assignment_info(v_d.o_fromassignmentid);
        SELECT * INTO v_ta FROM dbo.activitylog_assignment_info(v_d.o_toassignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_fa.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_ta.o_toid);
        SELECT * INTO v_via FROM dbo.activitylog_entity_info(v_d.o_facilitatorid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, viaid, vianame, viatype,
        roleid, rolename, viaroleid, viarolename, resourceid, resourcename, itemid, parentid, details
        ) VALUES (
        2, 2, 1, NEW.audit_validfrom,
        NEW.audit_changedby, dbo.activitylog_entity_name(NEW.audit_changedby), NEW.audit_changedbysystem, NEW.audit_changeoperation,
        v_fa.o_fromid, v_from.o_name, v_from.o_type, v_ta.o_toid, v_to.o_name, v_to.o_type,
        v_d.o_facilitatorid, v_via.o_name, v_via.o_type,
        v_fa.o_roleid, dbo.activitylog_role_name(v_fa.o_roleid),
        v_ta.o_roleid, dbo.activitylog_role_name(v_ta.o_roleid),
        NEW.resourceid, dbo.activitylog_resource_name(NEW.resourceid), NEW.id, NEW.delegationid,
        NULLIF(jsonb_strip_nulls(jsonb_build_object('assignmentResourceId', NEW.assignmentresourceid)), '{}'::jsonb)
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string DelegationResourceDeleteFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_delegationresource_delete_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        ctx RECORD;
        v_d RECORD;
        v_fa RECORD;
        v_ta RECORD;
        v_from RECORD;
        v_to RECORD;
        v_via RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO ctx FROM session_audit_context LIMIT 1;
        SELECT * INTO v_d FROM dbo.activitylog_delegation_info(OLD.delegationid);
        SELECT * INTO v_fa FROM dbo.activitylog_assignment_info(v_d.o_fromassignmentid);
        SELECT * INTO v_ta FROM dbo.activitylog_assignment_info(v_d.o_toassignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_fa.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_ta.o_toid);
        SELECT * INTO v_via FROM dbo.activitylog_entity_info(v_d.o_facilitatorid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, viaid, vianame, viatype,
        roleid, rolename, viaroleid, viarolename, resourceid, resourcename, itemid, parentid, details
        ) VALUES (
        2, 2, 3, now(),
        ctx.changed_by, dbo.activitylog_entity_name(ctx.changed_by), ctx.changed_by_system, ctx.change_operation_id,
        v_fa.o_fromid, v_from.o_name, v_from.o_type, v_ta.o_toid, v_to.o_name, v_to.o_type,
        v_d.o_facilitatorid, v_via.o_name, v_via.o_type,
        v_fa.o_roleid, dbo.activitylog_role_name(v_fa.o_roleid),
        v_ta.o_roleid, dbo.activitylog_role_name(v_ta.o_roleid),
        OLD.resourceid, dbo.activitylog_resource_name(OLD.resourceid), OLD.id, OLD.delegationid,
        NULLIF(jsonb_strip_nulls(jsonb_build_object('assignmentResourceId', OLD.assignmentresourceid)), '{}'::jsonb)
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string RequestAssignmentInsertFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_requestassignment_insert_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(NEW.fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(NEW.toid);
        INSERT INTO dbo.activitylog (
        "type", "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename, itemid, details
        ) VALUES (
        3, 1, NEW.audit_validfrom,
        NEW.audit_changedby, dbo.activitylog_entity_name(NEW.audit_changedby), NEW.audit_changedbysystem, NEW.audit_changeoperation,
        NEW.fromid, v_from.o_name, v_from.o_type, NEW.toid, v_to.o_name, v_to.o_type,
        NEW.roleid, dbo.activitylog_role_name(NEW.roleid), NEW.id,
        NULLIF(jsonb_strip_nulls(jsonb_build_object('requestedById', NEW.byid)), '{}'::jsonb)
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string RequestAssignmentDeleteFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_requestassignment_delete_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        ctx RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO ctx FROM session_audit_context LIMIT 1;
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(OLD.fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(OLD.toid);
        INSERT INTO dbo.activitylog (
        "type", "trigger", "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename, itemid, details
        ) VALUES (
        3, 3, now(),
        ctx.changed_by, dbo.activitylog_entity_name(ctx.changed_by), ctx.changed_by_system, ctx.change_operation_id,
        OLD.fromid, v_from.o_name, v_from.o_type, OLD.toid, v_to.o_name, v_to.o_type,
        OLD.roleid, dbo.activitylog_role_name(OLD.roleid), OLD.id,
        NULLIF(jsonb_strip_nulls(jsonb_build_object('requestedById', OLD.byid)), '{}'::jsonb)
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string RequestAssignmentPackageInsertFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_requestassignmentpackage_insert_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        v_ra RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO v_ra FROM dbo.activitylog_requestassignment_info(NEW.assignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_ra.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_ra.o_toid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", status, "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename,
        packageid, packagename, itemid, parentid, details
        ) VALUES (
        3, 1, 1, NEW.status, NEW.audit_validfrom,
        NEW.audit_changedby, dbo.activitylog_entity_name(NEW.audit_changedby), NEW.audit_changedbysystem, NEW.audit_changeoperation,
        v_ra.o_fromid, v_from.o_name, v_from.o_type, v_ra.o_toid, v_to.o_name, v_to.o_type,
        v_ra.o_roleid, dbo.activitylog_role_name(v_ra.o_roleid),
        NEW.packageid, dbo.activitylog_package_name(NEW.packageid), NEW.id, NEW.assignmentid,
        NULLIF(jsonb_strip_nulls(jsonb_build_object('requestedById', v_ra.o_byid)), '{}'::jsonb)
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string RequestAssignmentPackageUpdateFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_requestassignmentpackage_update_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        v_by uuid;
        v_bysystem uuid;
        v_operation text;
        v_ra RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT current_setting('app.changed_by', false) INTO v_by;
        SELECT current_setting('app.changed_by_system', false) INTO v_bysystem;
        SELECT current_setting('app.change_operation_id', false) INTO v_operation;
        SELECT * INTO v_ra FROM dbo.activitylog_requestassignment_info(NEW.assignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_ra.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_ra.o_toid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", status, "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename,
        packageid, packagename, itemid, parentid, details
        ) VALUES (
        3, 1, 2, NEW.status, now(),
        v_by, dbo.activitylog_entity_name(v_by), v_bysystem, v_operation,
        v_ra.o_fromid, v_from.o_name, v_from.o_type, v_ra.o_toid, v_to.o_name, v_to.o_type,
        v_ra.o_roleid, dbo.activitylog_role_name(v_ra.o_roleid),
        NEW.packageid, dbo.activitylog_package_name(NEW.packageid), NEW.id, NEW.assignmentid,
        NULLIF(jsonb_strip_nulls(jsonb_build_object('previousStatus', OLD.status, 'requestedById', v_ra.o_byid)), '{}'::jsonb)
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string RequestAssignmentPackageDeleteFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_requestassignmentpackage_delete_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        ctx RECORD;
        v_ra RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO ctx FROM session_audit_context LIMIT 1;
        SELECT * INTO v_ra FROM dbo.activitylog_requestassignment_info(OLD.assignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_ra.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_ra.o_toid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", status, "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename,
        packageid, packagename, itemid, parentid, details
        ) VALUES (
        3, 1, 3, OLD.status, now(),
        ctx.changed_by, dbo.activitylog_entity_name(ctx.changed_by), ctx.changed_by_system, ctx.change_operation_id,
        v_ra.o_fromid, v_from.o_name, v_from.o_type, v_ra.o_toid, v_to.o_name, v_to.o_type,
        v_ra.o_roleid, dbo.activitylog_role_name(v_ra.o_roleid),
        OLD.packageid, dbo.activitylog_package_name(OLD.packageid), OLD.id, OLD.assignmentid,
        NULLIF(jsonb_strip_nulls(jsonb_build_object('requestedById', v_ra.o_byid)), '{}'::jsonb)
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string RequestAssignmentResourceInsertFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_requestassignmentresource_insert_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        v_ra RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO v_ra FROM dbo.activitylog_requestassignment_info(NEW.assignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_ra.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_ra.o_toid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", status, "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename,
        resourceid, resourcename, itemid, parentid, details
        ) VALUES (
        3, 2, 1, NEW.status, NEW.audit_validfrom,
        NEW.audit_changedby, dbo.activitylog_entity_name(NEW.audit_changedby), NEW.audit_changedbysystem, NEW.audit_changeoperation,
        v_ra.o_fromid, v_from.o_name, v_from.o_type, v_ra.o_toid, v_to.o_name, v_to.o_type,
        v_ra.o_roleid, dbo.activitylog_role_name(v_ra.o_roleid),
        NEW.resourceid, dbo.activitylog_resource_name(NEW.resourceid), NEW.id, NEW.assignmentid,
        NULLIF(jsonb_strip_nulls(jsonb_build_object('action', NEW.action, 'requestedById', v_ra.o_byid)), '{}'::jsonb)
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string RequestAssignmentResourceUpdateFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_requestassignmentresource_update_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        v_by uuid;
        v_bysystem uuid;
        v_operation text;
        v_ra RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT current_setting('app.changed_by', false) INTO v_by;
        SELECT current_setting('app.changed_by_system', false) INTO v_bysystem;
        SELECT current_setting('app.change_operation_id', false) INTO v_operation;
        SELECT * INTO v_ra FROM dbo.activitylog_requestassignment_info(NEW.assignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_ra.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_ra.o_toid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", status, "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename,
        resourceid, resourcename, itemid, parentid, details
        ) VALUES (
        3, 2, 2, NEW.status, now(),
        v_by, dbo.activitylog_entity_name(v_by), v_bysystem, v_operation,
        v_ra.o_fromid, v_from.o_name, v_from.o_type, v_ra.o_toid, v_to.o_name, v_to.o_type,
        v_ra.o_roleid, dbo.activitylog_role_name(v_ra.o_roleid),
        NEW.resourceid, dbo.activitylog_resource_name(NEW.resourceid), NEW.id, NEW.assignmentid,
        NULLIF(jsonb_strip_nulls(jsonb_build_object('previousStatus', OLD.status, 'action', NEW.action, 'requestedById', v_ra.o_byid)), '{}'::jsonb)
        );
        RETURN NULL;
        END;
        $$;
        """;

    private const string RequestAssignmentResourceDeleteFn = """
        CREATE OR REPLACE FUNCTION dbo.activitylog_requestassignmentresource_delete_fn()
        RETURNS TRIGGER LANGUAGE plpgsql AS $$
        DECLARE
        ctx RECORD;
        v_ra RECORD;
        v_from RECORD;
        v_to RECORD;
        BEGIN
        IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;
        SELECT * INTO ctx FROM session_audit_context LIMIT 1;
        SELECT * INTO v_ra FROM dbo.activitylog_requestassignment_info(OLD.assignmentid);
        SELECT * INTO v_from FROM dbo.activitylog_entity_info(v_ra.o_fromid);
        SELECT * INTO v_to FROM dbo.activitylog_entity_info(v_ra.o_toid);
        INSERT INTO dbo.activitylog (
        "type", subtype, "trigger", status, "when", byid, byname, sourceid, operationid,
        fromid, fromname, fromtype, toid, toname, totype, roleid, rolename,
        resourceid, resourcename, itemid, parentid, details
        ) VALUES (
        3, 2, 3, OLD.status, now(),
        ctx.changed_by, dbo.activitylog_entity_name(ctx.changed_by), ctx.changed_by_system, ctx.change_operation_id,
        v_ra.o_fromid, v_from.o_name, v_from.o_type, v_ra.o_toid, v_to.o_name, v_to.o_type,
        v_ra.o_roleid, dbo.activitylog_role_name(v_ra.o_roleid),
        OLD.resourceid, dbo.activitylog_resource_name(OLD.resourceid), OLD.id, OLD.assignmentid,
        NULLIF(jsonb_strip_nulls(jsonb_build_object('action', OLD.action, 'requestedById', v_ra.o_byid)), '{}'::jsonb)
        );
        RETURN NULL;
        END;
        $$;
        """;
}
