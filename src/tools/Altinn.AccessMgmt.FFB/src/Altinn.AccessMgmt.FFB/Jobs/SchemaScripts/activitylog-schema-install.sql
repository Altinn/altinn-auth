START TRANSACTION;
-- Support functions for dbo.activitylog. Idempotent; must run before the activitylog table is
-- created (dbo.uuid_generate_v7 is its id default) and before any activity log trigger fires.
-- The *_info/_name resolvers look up the live row first and fall back to the dbo_history audit
-- tables, so they still resolve rows that were removed earlier in the same cascading delete.

CREATE OR REPLACE FUNCTION dbo.uuid_generate_v7()
RETURNS uuid
LANGUAGE plpgsql
VOLATILE
AS $$
BEGIN
    -- Random v4 uuid overlaid with a millisecond timestamp, version bits set to 7.
    RETURN encode(
        set_bit(
            set_bit(
                overlay(uuid_send(gen_random_uuid())
                        placing substring(int8send(floor(extract(epoch FROM clock_timestamp()) * 1000)::bigint) FROM 3)
                        FROM 1 FOR 6),
                52, 1),
            53, 1),
        'hex')::uuid;
END;
$$;

CREATE OR REPLACE FUNCTION dbo.activitylog_entity_info(p_id uuid, OUT o_name text, OUT o_type text)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    IF p_id IS NULL THEN
        RETURN;
    END IF;

    SELECT e.name, et.name INTO o_name, o_type
    FROM dbo.entity e
    LEFT JOIN dbo.entitytype et ON et.id = e.typeid
    WHERE e.id = p_id;

    IF NOT FOUND THEN
        SELECT h.name, et.name INTO o_name, o_type
        FROM dbo_history.auditentity h
        LEFT JOIN dbo.entitytype et ON et.id = h.typeid
        WHERE h.id = p_id
        ORDER BY h.audit_validto DESC
        LIMIT 1;
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION dbo.activitylog_entity_name(p_id uuid)
RETURNS text
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_info RECORD;
BEGIN
    SELECT * INTO v_info FROM dbo.activitylog_entity_info(p_id);
    RETURN v_info.o_name;
END;
$$;

CREATE OR REPLACE FUNCTION dbo.activitylog_role_name(p_id uuid)
RETURNS text
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_name text;
BEGIN
    IF p_id IS NULL THEN
        RETURN NULL;
    END IF;

    SELECT name INTO v_name FROM dbo.role WHERE id = p_id;

    IF NOT FOUND THEN
        SELECT name INTO v_name
        FROM dbo_history.auditrole
        WHERE id = p_id
        ORDER BY audit_validto DESC
        LIMIT 1;
    END IF;

    RETURN v_name;
END;
$$;

CREATE OR REPLACE FUNCTION dbo.activitylog_package_name(p_id uuid)
RETURNS text
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_name text;
BEGIN
    IF p_id IS NULL THEN
        RETURN NULL;
    END IF;

    SELECT name INTO v_name FROM dbo.package WHERE id = p_id;

    IF NOT FOUND THEN
        SELECT name INTO v_name
        FROM dbo_history.auditpackage
        WHERE id = p_id
        ORDER BY audit_validto DESC
        LIMIT 1;
    END IF;

    RETURN v_name;
END;
$$;

CREATE OR REPLACE FUNCTION dbo.activitylog_resource_name(p_id uuid)
RETURNS text
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_name text;
BEGIN
    IF p_id IS NULL THEN
        RETURN NULL;
    END IF;

    SELECT name INTO v_name FROM dbo.resource WHERE id = p_id;

    IF NOT FOUND THEN
        SELECT name INTO v_name
        FROM dbo_history.auditresource
        WHERE id = p_id
        ORDER BY audit_validto DESC
        LIMIT 1;
    END IF;

    RETURN v_name;
END;
$$;

CREATE OR REPLACE FUNCTION dbo.activitylog_assignment_info(p_id uuid, OUT o_fromid uuid, OUT o_toid uuid, OUT o_roleid uuid)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    IF p_id IS NULL THEN
        RETURN;
    END IF;

    SELECT fromid, toid, roleid INTO o_fromid, o_toid, o_roleid
    FROM dbo.assignment
    WHERE id = p_id;

    IF NOT FOUND THEN
        SELECT fromid, toid, roleid INTO o_fromid, o_toid, o_roleid
        FROM dbo_history.auditassignment
        WHERE id = p_id
        ORDER BY audit_validto DESC
        LIMIT 1;
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION dbo.activitylog_delegation_info(p_id uuid, OUT o_fromassignmentid uuid, OUT o_toassignmentid uuid, OUT o_facilitatorid uuid)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    IF p_id IS NULL THEN
        RETURN;
    END IF;

    SELECT fromid, toid, facilitatorid INTO o_fromassignmentid, o_toassignmentid, o_facilitatorid
    FROM dbo.delegation
    WHERE id = p_id;

    IF NOT FOUND THEN
        SELECT fromid, toid, facilitatorid INTO o_fromassignmentid, o_toassignmentid, o_facilitatorid
        FROM dbo_history.auditdelegation
        WHERE id = p_id
        ORDER BY audit_validto DESC
        LIMIT 1;
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION dbo.activitylog_requestassignment_info(p_id uuid, OUT o_fromid uuid, OUT o_toid uuid, OUT o_roleid uuid, OUT o_byid uuid)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    IF p_id IS NULL THEN
        RETURN;
    END IF;

    SELECT fromid, toid, roleid, byid INTO o_fromid, o_toid, o_roleid, o_byid
    FROM dbo.requestassignment
    WHERE id = p_id;

    IF NOT FOUND THEN
        SELECT fromid, toid, roleid, byid INTO o_fromid, o_toid, o_roleid, o_byid
        FROM dbo_history.auditrequestassignment
        WHERE id = p_id
        ORDER BY audit_validto DESC
        LIMIT 1;
    END IF;
END;
$$;

-- Creates any missing monthly partitions of dbo.activitylog covering [p_from, p_until].
-- SECURITY DEFINER: partition creation needs CREATE on schema dbo, which only the admin role
-- has; the function is owned by the migration role so jobs can call it with app credentials.
CREATE OR REPLACE FUNCTION dbo.activitylog_ensure_partitions(p_from date, p_until date)
RETURNS int
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, dbo
AS $$
DECLARE
    v_month date := date_trunc('month', p_from)::date;
    v_next date;
    v_name text;
    v_created int := 0;
BEGIN
    WHILE v_month <= p_until LOOP
        v_next := (v_month + interval '1 month')::date;
        v_name := 'activitylog_p' || to_char(v_month, 'YYYYMM');
        IF to_regclass('dbo.' || v_name) IS NULL THEN
            BEGIN
                EXECUTE format(
                    'CREATE TABLE dbo.%I PARTITION OF dbo.activitylog FOR VALUES FROM (%L) TO (%L)',
                    v_name,
                    v_month::text || ' 00:00:00+00',
                    v_next::text || ' 00:00:00+00');
                v_created := v_created + 1;
            EXCEPTION WHEN OTHERS THEN
                -- Overlap with the default partition (or a concurrent creator) must not abort the rest.
                RAISE WARNING 'activitylog_ensure_partitions: skipped % (%)', v_name, SQLERRM;
            END;
        END IF;
        v_month := v_next;
    END LOOP;
    RETURN v_created;
END;
$$;

REVOKE ALL ON FUNCTION dbo.activitylog_ensure_partitions(date, date) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION dbo.activitylog_ensure_partitions(date, date) TO platform_authorization, platform_authorization_admin;

CREATE OR REPLACE FUNCTION dbo.activitylog_ensure_month_partitions(p_months_ahead int DEFAULT 24)
RETURNS int
LANGUAGE sql
SECURITY DEFINER
SET search_path = pg_catalog, dbo
AS $$
    SELECT dbo.activitylog_ensure_partitions(now()::date, (now() + make_interval(months => p_months_ahead))::date);
$$;

REVOKE ALL ON FUNCTION dbo.activitylog_ensure_month_partitions(int) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION dbo.activitylog_ensure_month_partitions(int) TO platform_authorization, platform_authorization_admin;


CREATE TABLE dbo.activitylog (
    id uuid NOT NULL DEFAULT (dbo.uuid_generate_v7()),
    "when" timestamp with time zone NOT NULL,
    type integer NOT NULL,
    subtype integer,
    trigger integer NOT NULL,
    status integer,
    byid uuid,
    byname text,
    sourceid uuid,
    operationid text,
    fromid uuid,
    fromname text,
    fromtype text,
    toid uuid,
    toname text,
    totype text,
    viaid uuid,
    vianame text,
    viatype text,
    roleid uuid,
    rolename text,
    viaroleid uuid,
    viarolename text,
    packageid uuid,
    packagename text,
    resourceid uuid,
    resourcename text,
    instanceid text,
    itemid uuid NOT NULL,
    parentid uuid,
    details jsonb,
    CONSTRAINT pk_activitylog PRIMARY KEY ("when", id)
) PARTITION BY RANGE ("when");

GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.activitylog TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.activitylog TO platform_authorization_admin;


CREATE TABLE dbo.activitylogbackfillprogress (
    source text NOT NULL,
    cutoff timestamp with time zone NOT NULL,
    cursor timestamp with time zone,
    completedat timestamp with time zone,
    CONSTRAINT pk_activitylogbackfillprogress PRIMARY KEY (source)
);

GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.activitylogbackfillprogress TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.activitylogbackfillprogress TO platform_authorization_admin;


CREATE INDEX ix_activitylog_fromid_when ON dbo.activitylog (fromid, "when");

CREATE INDEX ix_activitylog_itemid ON dbo.activitylog (itemid);

CREATE INDEX ix_activitylog_parentid ON dbo.activitylog (parentid);

CREATE INDEX ix_activitylog_toid_when ON dbo.activitylog (toid, "when");

CREATE OR REPLACE FUNCTION dbo.audit_assignment_insert_fn() returns TRIGGER language plpgsql AS $$
BEGIN
DECLARE
changed_by UUID;
changed_by_system UUID;
change_operation_id text;
BEGIN
SELECT current_setting('app.changed_by', false) INTO changed_by;
SELECT current_setting('app.changed_by_system', false) INTO changed_by_system;
SELECT current_setting('app.change_operation_id', false) INTO change_operation_id;
IF NEW.audit_changedby IS NULL THEN NEW.audit_changedby := changed_by; END IF;
IF NEW.audit_changedbysystem IS NULL THEN NEW.audit_changedbysystem := changed_by_system; END IF;
IF NEW.audit_changeoperation IS NULL THEN NEW.audit_changeoperation := change_operation_id; END IF;
IF NEW.audit_validfrom IS NULL THEN NEW.audit_validfrom := now(); END IF;
RETURN NEW;
END;
END;
$$;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_assignment_insert_trg' AND t.tgrelid = to_regclass('dbo.assignment')) THEN
CREATE OR REPLACE TRIGGER audit_assignment_insert_trg BEFORE INSERT OR UPDATE ON dbo.assignment
FOR EACH ROW EXECUTE FUNCTION dbo.audit_assignment_insert_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_assignment_update_fn()
RETURNS TRIGGER AS $$
BEGIN
INSERT INTO dbo_history.auditassignment (
fromid,id,roleid,toid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.fromid,OLD.id,OLD.roleid,OLD.toid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_assignment_update_trg' AND t.tgrelid = to_regclass('dbo.assignment')) THEN
CREATE OR REPLACE TRIGGER audit_assignment_update_trg AFTER UPDATE ON dbo.assignment
FOR EACH ROW EXECUTE FUNCTION dbo.audit_assignment_update_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_assignment_delete_fn()
RETURNS TRIGGER AS $$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditassignment (
fromid,id,roleid,toid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.fromid,OLD.id,OLD.roleid,OLD.toid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_assignment_delete_trg' AND t.tgrelid = to_regclass('dbo.assignment')) THEN
CREATE OR REPLACE TRIGGER audit_assignment_delete_trg AFTER DELETE ON dbo.assignment
FOR EACH ROW EXECUTE FUNCTION dbo.audit_assignment_delete_fn();
END IF; END $$;


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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_assignment_insert_trg' AND t.tgrelid = to_regclass('dbo.assignment')) THEN
CREATE OR REPLACE TRIGGER activitylog_assignment_insert_trg AFTER INSERT ON dbo.assignment
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_assignment_insert_fn();
END IF; END $$;

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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_assignment_delete_trg' AND t.tgrelid = to_regclass('dbo.assignment')) THEN
CREATE OR REPLACE TRIGGER activitylog_assignment_delete_trg AFTER DELETE ON dbo.assignment
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_assignment_delete_fn();
END IF; END $$;

GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.assignment TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.assignment TO platform_authorization_admin;


CREATE OR REPLACE FUNCTION dbo.audit_assignmentpackage_insert_fn() returns TRIGGER language plpgsql AS $$
BEGIN
DECLARE
changed_by UUID;
changed_by_system UUID;
change_operation_id text;
BEGIN
SELECT current_setting('app.changed_by', false) INTO changed_by;
SELECT current_setting('app.changed_by_system', false) INTO changed_by_system;
SELECT current_setting('app.change_operation_id', false) INTO change_operation_id;
IF NEW.audit_changedby IS NULL THEN NEW.audit_changedby := changed_by; END IF;
IF NEW.audit_changedbysystem IS NULL THEN NEW.audit_changedbysystem := changed_by_system; END IF;
IF NEW.audit_changeoperation IS NULL THEN NEW.audit_changeoperation := change_operation_id; END IF;
IF NEW.audit_validfrom IS NULL THEN NEW.audit_validfrom := now(); END IF;
RETURN NEW;
END;
END;
$$;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_assignmentpackage_insert_trg' AND t.tgrelid = to_regclass('dbo.assignmentpackage')) THEN
CREATE OR REPLACE TRIGGER audit_assignmentpackage_insert_trg BEFORE INSERT OR UPDATE ON dbo.assignmentpackage
FOR EACH ROW EXECUTE FUNCTION dbo.audit_assignmentpackage_insert_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_assignmentpackage_update_fn()
RETURNS TRIGGER AS $$
BEGIN
INSERT INTO dbo_history.auditassignmentpackage (
assignmentid,id,packageid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.assignmentid,OLD.id,OLD.packageid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_assignmentpackage_update_trg' AND t.tgrelid = to_regclass('dbo.assignmentpackage')) THEN
CREATE OR REPLACE TRIGGER audit_assignmentpackage_update_trg AFTER UPDATE ON dbo.assignmentpackage
FOR EACH ROW EXECUTE FUNCTION dbo.audit_assignmentpackage_update_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_assignmentpackage_delete_fn()
RETURNS TRIGGER AS $$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditassignmentpackage (
assignmentid,id,packageid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.assignmentid,OLD.id,OLD.packageid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_assignmentpackage_delete_trg' AND t.tgrelid = to_regclass('dbo.assignmentpackage')) THEN
CREATE OR REPLACE TRIGGER audit_assignmentpackage_delete_trg AFTER DELETE ON dbo.assignmentpackage
FOR EACH ROW EXECUTE FUNCTION dbo.audit_assignmentpackage_delete_fn();
END IF; END $$;


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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_assignmentpackage_insert_trg' AND t.tgrelid = to_regclass('dbo.assignmentpackage')) THEN
CREATE OR REPLACE TRIGGER activitylog_assignmentpackage_insert_trg AFTER INSERT ON dbo.assignmentpackage
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_assignmentpackage_insert_fn();
END IF; END $$;

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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_assignmentpackage_delete_trg' AND t.tgrelid = to_regclass('dbo.assignmentpackage')) THEN
CREATE OR REPLACE TRIGGER activitylog_assignmentpackage_delete_trg AFTER DELETE ON dbo.assignmentpackage
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_assignmentpackage_delete_fn();
END IF; END $$;

GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.assignmentpackage TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.assignmentpackage TO platform_authorization_admin;


CREATE OR REPLACE FUNCTION dbo.audit_assignmentresource_insert_fn() returns TRIGGER language plpgsql AS $$
BEGIN
DECLARE
changed_by UUID;
changed_by_system UUID;
change_operation_id text;
BEGIN
SELECT current_setting('app.changed_by', false) INTO changed_by;
SELECT current_setting('app.changed_by_system', false) INTO changed_by_system;
SELECT current_setting('app.change_operation_id', false) INTO change_operation_id;
IF NEW.audit_changedby IS NULL THEN NEW.audit_changedby := changed_by; END IF;
IF NEW.audit_changedbysystem IS NULL THEN NEW.audit_changedbysystem := changed_by_system; END IF;
IF NEW.audit_changeoperation IS NULL THEN NEW.audit_changeoperation := change_operation_id; END IF;
IF NEW.audit_validfrom IS NULL THEN NEW.audit_validfrom := now(); END IF;
RETURN NEW;
END;
END;
$$;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_assignmentresource_insert_trg' AND t.tgrelid = to_regclass('dbo.assignmentresource')) THEN
CREATE OR REPLACE TRIGGER audit_assignmentresource_insert_trg BEFORE INSERT OR UPDATE ON dbo.assignmentresource
FOR EACH ROW EXECUTE FUNCTION dbo.audit_assignmentresource_insert_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_assignmentresource_update_fn()
RETURNS TRIGGER AS $$
BEGIN
INSERT INTO dbo_history.auditassignmentresource (
assignmentid,delegationchangeid,id,policypath,policyversion,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.assignmentid,OLD.delegationchangeid,OLD.id,OLD.policypath,OLD.policyversion,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_assignmentresource_update_trg' AND t.tgrelid = to_regclass('dbo.assignmentresource')) THEN
CREATE OR REPLACE TRIGGER audit_assignmentresource_update_trg AFTER UPDATE ON dbo.assignmentresource
FOR EACH ROW EXECUTE FUNCTION dbo.audit_assignmentresource_update_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_assignmentresource_delete_fn()
RETURNS TRIGGER AS $$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditassignmentresource (
assignmentid,delegationchangeid,id,policypath,policyversion,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.assignmentid,OLD.delegationchangeid,OLD.id,OLD.policypath,OLD.policyversion,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_assignmentresource_delete_trg' AND t.tgrelid = to_regclass('dbo.assignmentresource')) THEN
CREATE OR REPLACE TRIGGER audit_assignmentresource_delete_trg AFTER DELETE ON dbo.assignmentresource
FOR EACH ROW EXECUTE FUNCTION dbo.audit_assignmentresource_delete_fn();
END IF; END $$;


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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_assignmentresource_insert_trg' AND t.tgrelid = to_regclass('dbo.assignmentresource')) THEN
CREATE OR REPLACE TRIGGER activitylog_assignmentresource_insert_trg AFTER INSERT ON dbo.assignmentresource
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_assignmentresource_insert_fn();
END IF; END $$;

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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_assignmentresource_delete_trg' AND t.tgrelid = to_regclass('dbo.assignmentresource')) THEN
CREATE OR REPLACE TRIGGER activitylog_assignmentresource_delete_trg AFTER DELETE ON dbo.assignmentresource
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_assignmentresource_delete_fn();
END IF; END $$;

GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.assignmentresource TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.assignmentresource TO platform_authorization_admin;


CREATE OR REPLACE FUNCTION dbo.audit_assignmentinstance_insert_fn() returns TRIGGER language plpgsql AS $$
BEGIN
DECLARE
changed_by UUID;
changed_by_system UUID;
change_operation_id text;
BEGIN
SELECT current_setting('app.changed_by', false) INTO changed_by;
SELECT current_setting('app.changed_by_system', false) INTO changed_by_system;
SELECT current_setting('app.change_operation_id', false) INTO change_operation_id;
IF NEW.audit_changedby IS NULL THEN NEW.audit_changedby := changed_by; END IF;
IF NEW.audit_changedbysystem IS NULL THEN NEW.audit_changedbysystem := changed_by_system; END IF;
IF NEW.audit_changeoperation IS NULL THEN NEW.audit_changeoperation := change_operation_id; END IF;
IF NEW.audit_validfrom IS NULL THEN NEW.audit_validfrom := now(); END IF;
RETURN NEW;
END;
END;
$$;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_assignmentinstance_insert_trg' AND t.tgrelid = to_regclass('dbo.assignmentinstance')) THEN
CREATE OR REPLACE TRIGGER audit_assignmentinstance_insert_trg BEFORE INSERT OR UPDATE ON dbo.assignmentinstance
FOR EACH ROW EXECUTE FUNCTION dbo.audit_assignmentinstance_insert_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_assignmentinstance_update_fn()
RETURNS TRIGGER AS $$
BEGIN
INSERT INTO dbo_history.auditassignmentinstance (
assignmentid,delegationchangeid,id,instanceid,instancesourcetypeid,policypath,policyversion,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.assignmentid,OLD.delegationchangeid,OLD.id,OLD.instanceid,OLD.instancesourcetypeid,OLD.policypath,OLD.policyversion,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_assignmentinstance_update_trg' AND t.tgrelid = to_regclass('dbo.assignmentinstance')) THEN
CREATE OR REPLACE TRIGGER audit_assignmentinstance_update_trg AFTER UPDATE ON dbo.assignmentinstance
FOR EACH ROW EXECUTE FUNCTION dbo.audit_assignmentinstance_update_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_assignmentinstance_delete_fn()
RETURNS TRIGGER AS $$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditassignmentinstance (
assignmentid,delegationchangeid,id,instanceid,instancesourcetypeid,policypath,policyversion,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.assignmentid,OLD.delegationchangeid,OLD.id,OLD.instanceid,OLD.instancesourcetypeid,OLD.policypath,OLD.policyversion,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_assignmentinstance_delete_trg' AND t.tgrelid = to_regclass('dbo.assignmentinstance')) THEN
CREATE OR REPLACE TRIGGER audit_assignmentinstance_delete_trg AFTER DELETE ON dbo.assignmentinstance
FOR EACH ROW EXECUTE FUNCTION dbo.audit_assignmentinstance_delete_fn();
END IF; END $$;


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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_assignmentinstance_insert_trg' AND t.tgrelid = to_regclass('dbo.assignmentinstance')) THEN
CREATE OR REPLACE TRIGGER activitylog_assignmentinstance_insert_trg AFTER INSERT ON dbo.assignmentinstance
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_assignmentinstance_insert_fn();
END IF; END $$;

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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_assignmentinstance_update_trg' AND t.tgrelid = to_regclass('dbo.assignmentinstance')) THEN
CREATE OR REPLACE TRIGGER activitylog_assignmentinstance_update_trg AFTER UPDATE ON dbo.assignmentinstance
FOR EACH ROW WHEN (OLD.assignmentid IS DISTINCT FROM NEW.assignmentid) EXECUTE FUNCTION dbo.activitylog_assignmentinstance_update_fn();
END IF; END $$;

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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_assignmentinstance_delete_trg' AND t.tgrelid = to_regclass('dbo.assignmentinstance')) THEN
CREATE OR REPLACE TRIGGER activitylog_assignmentinstance_delete_trg AFTER DELETE ON dbo.assignmentinstance
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_assignmentinstance_delete_fn();
END IF; END $$;

GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.assignmentinstance TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.assignmentinstance TO platform_authorization_admin;


CREATE OR REPLACE FUNCTION dbo.audit_delegation_insert_fn() returns TRIGGER language plpgsql AS $$
BEGIN
DECLARE
changed_by UUID;
changed_by_system UUID;
change_operation_id text;
BEGIN
SELECT current_setting('app.changed_by', false) INTO changed_by;
SELECT current_setting('app.changed_by_system', false) INTO changed_by_system;
SELECT current_setting('app.change_operation_id', false) INTO change_operation_id;
IF NEW.audit_changedby IS NULL THEN NEW.audit_changedby := changed_by; END IF;
IF NEW.audit_changedbysystem IS NULL THEN NEW.audit_changedbysystem := changed_by_system; END IF;
IF NEW.audit_changeoperation IS NULL THEN NEW.audit_changeoperation := change_operation_id; END IF;
IF NEW.audit_validfrom IS NULL THEN NEW.audit_validfrom := now(); END IF;
RETURN NEW;
END;
END;
$$;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_delegation_insert_trg' AND t.tgrelid = to_regclass('dbo.delegation')) THEN
CREATE OR REPLACE TRIGGER audit_delegation_insert_trg BEFORE INSERT OR UPDATE ON dbo.delegation
FOR EACH ROW EXECUTE FUNCTION dbo.audit_delegation_insert_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_delegation_update_fn()
RETURNS TRIGGER AS $$
BEGIN
INSERT INTO dbo_history.auditdelegation (
facilitatorid,fromid,id,toid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.facilitatorid,OLD.fromid,OLD.id,OLD.toid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_delegation_update_trg' AND t.tgrelid = to_regclass('dbo.delegation')) THEN
CREATE OR REPLACE TRIGGER audit_delegation_update_trg AFTER UPDATE ON dbo.delegation
FOR EACH ROW EXECUTE FUNCTION dbo.audit_delegation_update_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_delegation_delete_fn()
RETURNS TRIGGER AS $$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditdelegation (
facilitatorid,fromid,id,toid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.facilitatorid,OLD.fromid,OLD.id,OLD.toid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_delegation_delete_trg' AND t.tgrelid = to_regclass('dbo.delegation')) THEN
CREATE OR REPLACE TRIGGER audit_delegation_delete_trg AFTER DELETE ON dbo.delegation
FOR EACH ROW EXECUTE FUNCTION dbo.audit_delegation_delete_fn();
END IF; END $$;


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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_delegation_insert_trg' AND t.tgrelid = to_regclass('dbo.delegation')) THEN
CREATE OR REPLACE TRIGGER activitylog_delegation_insert_trg AFTER INSERT ON dbo.delegation
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_delegation_insert_fn();
END IF; END $$;

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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_delegation_delete_trg' AND t.tgrelid = to_regclass('dbo.delegation')) THEN
CREATE OR REPLACE TRIGGER activitylog_delegation_delete_trg AFTER DELETE ON dbo.delegation
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_delegation_delete_fn();
END IF; END $$;

GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.delegation TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.delegation TO platform_authorization_admin;


CREATE OR REPLACE FUNCTION dbo.audit_delegationpackage_insert_fn() returns TRIGGER language plpgsql AS $$
BEGIN
DECLARE
changed_by UUID;
changed_by_system UUID;
change_operation_id text;
BEGIN
SELECT current_setting('app.changed_by', false) INTO changed_by;
SELECT current_setting('app.changed_by_system', false) INTO changed_by_system;
SELECT current_setting('app.change_operation_id', false) INTO change_operation_id;
IF NEW.audit_changedby IS NULL THEN NEW.audit_changedby := changed_by; END IF;
IF NEW.audit_changedbysystem IS NULL THEN NEW.audit_changedbysystem := changed_by_system; END IF;
IF NEW.audit_changeoperation IS NULL THEN NEW.audit_changeoperation := change_operation_id; END IF;
IF NEW.audit_validfrom IS NULL THEN NEW.audit_validfrom := now(); END IF;
RETURN NEW;
END;
END;
$$;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_delegationpackage_insert_trg' AND t.tgrelid = to_regclass('dbo.delegationpackage')) THEN
CREATE OR REPLACE TRIGGER audit_delegationpackage_insert_trg BEFORE INSERT OR UPDATE ON dbo.delegationpackage
FOR EACH ROW EXECUTE FUNCTION dbo.audit_delegationpackage_insert_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_delegationpackage_update_fn()
RETURNS TRIGGER AS $$
BEGIN
INSERT INTO dbo_history.auditdelegationpackage (
assignmentpackageid,delegationid,id,packageid,rolepackageid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.assignmentpackageid,OLD.delegationid,OLD.id,OLD.packageid,OLD.rolepackageid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_delegationpackage_update_trg' AND t.tgrelid = to_regclass('dbo.delegationpackage')) THEN
CREATE OR REPLACE TRIGGER audit_delegationpackage_update_trg AFTER UPDATE ON dbo.delegationpackage
FOR EACH ROW EXECUTE FUNCTION dbo.audit_delegationpackage_update_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_delegationpackage_delete_fn()
RETURNS TRIGGER AS $$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditdelegationpackage (
assignmentpackageid,delegationid,id,packageid,rolepackageid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.assignmentpackageid,OLD.delegationid,OLD.id,OLD.packageid,OLD.rolepackageid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_delegationpackage_delete_trg' AND t.tgrelid = to_regclass('dbo.delegationpackage')) THEN
CREATE OR REPLACE TRIGGER audit_delegationpackage_delete_trg AFTER DELETE ON dbo.delegationpackage
FOR EACH ROW EXECUTE FUNCTION dbo.audit_delegationpackage_delete_fn();
END IF; END $$;


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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_delegationpackage_insert_trg' AND t.tgrelid = to_regclass('dbo.delegationpackage')) THEN
CREATE OR REPLACE TRIGGER activitylog_delegationpackage_insert_trg AFTER INSERT ON dbo.delegationpackage
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_delegationpackage_insert_fn();
END IF; END $$;

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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_delegationpackage_delete_trg' AND t.tgrelid = to_regclass('dbo.delegationpackage')) THEN
CREATE OR REPLACE TRIGGER activitylog_delegationpackage_delete_trg AFTER DELETE ON dbo.delegationpackage
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_delegationpackage_delete_fn();
END IF; END $$;

GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.delegationpackage TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.delegationpackage TO platform_authorization_admin;


CREATE OR REPLACE FUNCTION dbo.audit_delegationresource_insert_fn() returns TRIGGER language plpgsql AS $$
BEGIN
DECLARE
changed_by UUID;
changed_by_system UUID;
change_operation_id text;
BEGIN
SELECT current_setting('app.changed_by', false) INTO changed_by;
SELECT current_setting('app.changed_by_system', false) INTO changed_by_system;
SELECT current_setting('app.change_operation_id', false) INTO change_operation_id;
IF NEW.audit_changedby IS NULL THEN NEW.audit_changedby := changed_by; END IF;
IF NEW.audit_changedbysystem IS NULL THEN NEW.audit_changedbysystem := changed_by_system; END IF;
IF NEW.audit_changeoperation IS NULL THEN NEW.audit_changeoperation := change_operation_id; END IF;
IF NEW.audit_validfrom IS NULL THEN NEW.audit_validfrom := now(); END IF;
RETURN NEW;
END;
END;
$$;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_delegationresource_insert_trg' AND t.tgrelid = to_regclass('dbo.delegationresource')) THEN
CREATE OR REPLACE TRIGGER audit_delegationresource_insert_trg BEFORE INSERT OR UPDATE ON dbo.delegationresource
FOR EACH ROW EXECUTE FUNCTION dbo.audit_delegationresource_insert_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_delegationresource_update_fn()
RETURNS TRIGGER AS $$
BEGIN
INSERT INTO dbo_history.auditdelegationresource (
assignmentresourceid,delegationid,id,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.assignmentresourceid,OLD.delegationid,OLD.id,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_delegationresource_update_trg' AND t.tgrelid = to_regclass('dbo.delegationresource')) THEN
CREATE OR REPLACE TRIGGER audit_delegationresource_update_trg AFTER UPDATE ON dbo.delegationresource
FOR EACH ROW EXECUTE FUNCTION dbo.audit_delegationresource_update_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_delegationresource_delete_fn()
RETURNS TRIGGER AS $$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditdelegationresource (
assignmentresourceid,delegationid,id,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.assignmentresourceid,OLD.delegationid,OLD.id,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_delegationresource_delete_trg' AND t.tgrelid = to_regclass('dbo.delegationresource')) THEN
CREATE OR REPLACE TRIGGER audit_delegationresource_delete_trg AFTER DELETE ON dbo.delegationresource
FOR EACH ROW EXECUTE FUNCTION dbo.audit_delegationresource_delete_fn();
END IF; END $$;


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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_delegationresource_insert_trg' AND t.tgrelid = to_regclass('dbo.delegationresource')) THEN
CREATE OR REPLACE TRIGGER activitylog_delegationresource_insert_trg AFTER INSERT ON dbo.delegationresource
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_delegationresource_insert_fn();
END IF; END $$;

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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_delegationresource_delete_trg' AND t.tgrelid = to_regclass('dbo.delegationresource')) THEN
CREATE OR REPLACE TRIGGER activitylog_delegationresource_delete_trg AFTER DELETE ON dbo.delegationresource
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_delegationresource_delete_fn();
END IF; END $$;

GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.delegationresource TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.delegationresource TO platform_authorization_admin;


CREATE OR REPLACE FUNCTION dbo.audit_requestassignment_insert_fn() returns TRIGGER language plpgsql AS $$
BEGIN
DECLARE
changed_by UUID;
changed_by_system UUID;
change_operation_id text;
BEGIN
SELECT current_setting('app.changed_by', false) INTO changed_by;
SELECT current_setting('app.changed_by_system', false) INTO changed_by_system;
SELECT current_setting('app.change_operation_id', false) INTO change_operation_id;
IF NEW.audit_changedby IS NULL THEN NEW.audit_changedby := changed_by; END IF;
IF NEW.audit_changedbysystem IS NULL THEN NEW.audit_changedbysystem := changed_by_system; END IF;
IF NEW.audit_changeoperation IS NULL THEN NEW.audit_changeoperation := change_operation_id; END IF;
IF NEW.audit_validfrom IS NULL THEN NEW.audit_validfrom := now(); END IF;
RETURN NEW;
END;
END;
$$;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_requestassignment_insert_trg' AND t.tgrelid = to_regclass('dbo.requestassignment')) THEN
CREATE OR REPLACE TRIGGER audit_requestassignment_insert_trg BEFORE INSERT OR UPDATE ON dbo.requestassignment
FOR EACH ROW EXECUTE FUNCTION dbo.audit_requestassignment_insert_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_requestassignment_update_fn()
RETURNS TRIGGER AS $$
BEGIN
INSERT INTO dbo_history.auditrequestassignment (
byid,fromid,id,roleid,toid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.byid,OLD.fromid,OLD.id,OLD.roleid,OLD.toid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_requestassignment_update_trg' AND t.tgrelid = to_regclass('dbo.requestassignment')) THEN
CREATE OR REPLACE TRIGGER audit_requestassignment_update_trg AFTER UPDATE ON dbo.requestassignment
FOR EACH ROW EXECUTE FUNCTION dbo.audit_requestassignment_update_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_requestassignment_delete_fn()
RETURNS TRIGGER AS $$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditrequestassignment (
byid,fromid,id,roleid,toid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.byid,OLD.fromid,OLD.id,OLD.roleid,OLD.toid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_requestassignment_delete_trg' AND t.tgrelid = to_regclass('dbo.requestassignment')) THEN
CREATE OR REPLACE TRIGGER audit_requestassignment_delete_trg AFTER DELETE ON dbo.requestassignment
FOR EACH ROW EXECUTE FUNCTION dbo.audit_requestassignment_delete_fn();
END IF; END $$;


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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_requestassignment_insert_trg' AND t.tgrelid = to_regclass('dbo.requestassignment')) THEN
CREATE OR REPLACE TRIGGER activitylog_requestassignment_insert_trg AFTER INSERT ON dbo.requestassignment
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_requestassignment_insert_fn();
END IF; END $$;

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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_requestassignment_delete_trg' AND t.tgrelid = to_regclass('dbo.requestassignment')) THEN
CREATE OR REPLACE TRIGGER activitylog_requestassignment_delete_trg AFTER DELETE ON dbo.requestassignment
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_requestassignment_delete_fn();
END IF; END $$;

GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.requestassignment TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.requestassignment TO platform_authorization_admin;


CREATE OR REPLACE FUNCTION dbo.audit_requestassignmentpackage_insert_fn() returns TRIGGER language plpgsql AS $$
BEGIN
DECLARE
changed_by UUID;
changed_by_system UUID;
change_operation_id text;
BEGIN
SELECT current_setting('app.changed_by', false) INTO changed_by;
SELECT current_setting('app.changed_by_system', false) INTO changed_by_system;
SELECT current_setting('app.change_operation_id', false) INTO change_operation_id;
IF NEW.audit_changedby IS NULL THEN NEW.audit_changedby := changed_by; END IF;
IF NEW.audit_changedbysystem IS NULL THEN NEW.audit_changedbysystem := changed_by_system; END IF;
IF NEW.audit_changeoperation IS NULL THEN NEW.audit_changeoperation := change_operation_id; END IF;
IF NEW.audit_validfrom IS NULL THEN NEW.audit_validfrom := now(); END IF;
RETURN NEW;
END;
END;
$$;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_requestassignmentpackage_insert_trg' AND t.tgrelid = to_regclass('dbo.requestassignmentpackage')) THEN
CREATE OR REPLACE TRIGGER audit_requestassignmentpackage_insert_trg BEFORE INSERT OR UPDATE ON dbo.requestassignmentpackage
FOR EACH ROW EXECUTE FUNCTION dbo.audit_requestassignmentpackage_insert_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_requestassignmentpackage_update_fn()
RETURNS TRIGGER AS $$
BEGIN
INSERT INTO dbo_history.auditrequestassignmentpackage (
assignmentid,id,packageid,status,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.assignmentid,OLD.id,OLD.packageid,OLD.status,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_requestassignmentpackage_update_trg' AND t.tgrelid = to_regclass('dbo.requestassignmentpackage')) THEN
CREATE OR REPLACE TRIGGER audit_requestassignmentpackage_update_trg AFTER UPDATE ON dbo.requestassignmentpackage
FOR EACH ROW EXECUTE FUNCTION dbo.audit_requestassignmentpackage_update_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_requestassignmentpackage_delete_fn()
RETURNS TRIGGER AS $$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditrequestassignmentpackage (
assignmentid,id,packageid,status,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.assignmentid,OLD.id,OLD.packageid,OLD.status,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_requestassignmentpackage_delete_trg' AND t.tgrelid = to_regclass('dbo.requestassignmentpackage')) THEN
CREATE OR REPLACE TRIGGER audit_requestassignmentpackage_delete_trg AFTER DELETE ON dbo.requestassignmentpackage
FOR EACH ROW EXECUTE FUNCTION dbo.audit_requestassignmentpackage_delete_fn();
END IF; END $$;


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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_requestassignmentpackage_insert_trg' AND t.tgrelid = to_regclass('dbo.requestassignmentpackage')) THEN
CREATE OR REPLACE TRIGGER activitylog_requestassignmentpackage_insert_trg AFTER INSERT ON dbo.requestassignmentpackage
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_requestassignmentpackage_insert_fn();
END IF; END $$;

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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_requestassignmentpackage_update_trg' AND t.tgrelid = to_regclass('dbo.requestassignmentpackage')) THEN
CREATE OR REPLACE TRIGGER activitylog_requestassignmentpackage_update_trg AFTER UPDATE ON dbo.requestassignmentpackage
FOR EACH ROW WHEN (OLD.status IS DISTINCT FROM NEW.status) EXECUTE FUNCTION dbo.activitylog_requestassignmentpackage_update_fn();
END IF; END $$;

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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_requestassignmentpackage_delete_trg' AND t.tgrelid = to_regclass('dbo.requestassignmentpackage')) THEN
CREATE OR REPLACE TRIGGER activitylog_requestassignmentpackage_delete_trg AFTER DELETE ON dbo.requestassignmentpackage
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_requestassignmentpackage_delete_fn();
END IF; END $$;

GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.requestassignmentpackage TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.requestassignmentpackage TO platform_authorization_admin;


CREATE OR REPLACE FUNCTION dbo.audit_requestassignmentresource_insert_fn() returns TRIGGER language plpgsql AS $$
BEGIN
DECLARE
changed_by UUID;
changed_by_system UUID;
change_operation_id text;
BEGIN
SELECT current_setting('app.changed_by', false) INTO changed_by;
SELECT current_setting('app.changed_by_system', false) INTO changed_by_system;
SELECT current_setting('app.change_operation_id', false) INTO change_operation_id;
IF NEW.audit_changedby IS NULL THEN NEW.audit_changedby := changed_by; END IF;
IF NEW.audit_changedbysystem IS NULL THEN NEW.audit_changedbysystem := changed_by_system; END IF;
IF NEW.audit_changeoperation IS NULL THEN NEW.audit_changeoperation := change_operation_id; END IF;
IF NEW.audit_validfrom IS NULL THEN NEW.audit_validfrom := now(); END IF;
RETURN NEW;
END;
END;
$$;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_requestassignmentresource_insert_trg' AND t.tgrelid = to_regclass('dbo.requestassignmentresource')) THEN
CREATE OR REPLACE TRIGGER audit_requestassignmentresource_insert_trg BEFORE INSERT OR UPDATE ON dbo.requestassignmentresource
FOR EACH ROW EXECUTE FUNCTION dbo.audit_requestassignmentresource_insert_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_requestassignmentresource_update_fn()
RETURNS TRIGGER AS $$
BEGIN
INSERT INTO dbo_history.auditrequestassignmentresource (
action,assignmentid,id,resourceid,status,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.action,OLD.assignmentid,OLD.id,OLD.resourceid,OLD.status,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_requestassignmentresource_update_trg' AND t.tgrelid = to_regclass('dbo.requestassignmentresource')) THEN
CREATE OR REPLACE TRIGGER audit_requestassignmentresource_update_trg AFTER UPDATE ON dbo.requestassignmentresource
FOR EACH ROW EXECUTE FUNCTION dbo.audit_requestassignmentresource_update_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_requestassignmentresource_delete_fn()
RETURNS TRIGGER AS $$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditrequestassignmentresource (
action,assignmentid,id,resourceid,status,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.action,OLD.assignmentid,OLD.id,OLD.resourceid,OLD.status,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_requestassignmentresource_delete_trg' AND t.tgrelid = to_regclass('dbo.requestassignmentresource')) THEN
CREATE OR REPLACE TRIGGER audit_requestassignmentresource_delete_trg AFTER DELETE ON dbo.requestassignmentresource
FOR EACH ROW EXECUTE FUNCTION dbo.audit_requestassignmentresource_delete_fn();
END IF; END $$;


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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_requestassignmentresource_insert_trg' AND t.tgrelid = to_regclass('dbo.requestassignmentresource')) THEN
CREATE OR REPLACE TRIGGER activitylog_requestassignmentresource_insert_trg AFTER INSERT ON dbo.requestassignmentresource
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_requestassignmentresource_insert_fn();
END IF; END $$;

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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_requestassignmentresource_update_trg' AND t.tgrelid = to_regclass('dbo.requestassignmentresource')) THEN
CREATE OR REPLACE TRIGGER activitylog_requestassignmentresource_update_trg AFTER UPDATE ON dbo.requestassignmentresource
FOR EACH ROW WHEN (OLD.status IS DISTINCT FROM NEW.status) EXECUTE FUNCTION dbo.activitylog_requestassignmentresource_update_fn();
END IF; END $$;

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

DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'activitylog_requestassignmentresource_delete_trg' AND t.tgrelid = to_regclass('dbo.requestassignmentresource')) THEN
CREATE OR REPLACE TRIGGER activitylog_requestassignmentresource_delete_trg AFTER DELETE ON dbo.requestassignmentresource
FOR EACH ROW EXECUTE FUNCTION dbo.activitylog_requestassignmentresource_delete_fn();
END IF; END $$;

GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.requestassignmentresource TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.requestassignmentresource TO platform_authorization_admin;


-- Initial partitions for dbo.activitylog and the backfill progress seed. Idempotent.
-- Yearly partitions carry the backfilled history (2000-2025), monthly partitions carry live
-- data from 2026 on, and a default partition catches anything outside the created ranges so
-- an out-of-range "when" can never make a business transaction fail.

DO $$
DECLARE
    v_year int;
BEGIN
    FOR v_year IN 2000..2025 LOOP
        IF to_regclass(format('dbo.activitylog_y%s', v_year)) IS NULL THEN
            EXECUTE format(
                'CREATE TABLE dbo.activitylog_y%s PARTITION OF dbo.activitylog FOR VALUES FROM (%L) TO (%L)',
                v_year,
                v_year::text || '-01-01 00:00:00+00',
                (v_year + 1)::text || '-01-01 00:00:00+00');
        END IF;
    END LOOP;
END;
$$;

-- Monthly partitions from the fixed 2026-01 boundary (where the yearly range ends) until two
-- years ahead; the partition maintenance job keeps extending from here.
SELECT dbo.activitylog_ensure_partitions('2026-01-01'::date, (now() + interval '24 months')::date);

DO $$
BEGIN
    IF to_regclass('dbo.activitylog_default') IS NULL THEN
        CREATE TABLE dbo.activitylog_default PARTITION OF dbo.activitylog DEFAULT;
    END IF;
END;
$$;

-- Backfill cutoff = the moment the activity log triggers went live. The backfill job only
-- synthesizes events strictly before the cutoff, so it can never duplicate trigger-written rows.
INSERT INTO dbo.activitylogbackfillprogress (source, cutoff)
VALUES
    ('assignment', now()),
    ('assignmentpackage', now()),
    ('assignmentresource', now()),
    ('assignmentinstance', now()),
    ('delegation', now()),
    ('delegationpackage', now()),
    ('delegationresource', now()),
    ('requestassignment', now()),
    ('requestassignmentpackage', now()),
    ('requestassignmentresource', now())
ON CONFLICT (source) DO NOTHING;



COMMIT;

START TRANSACTION;
CREATE TABLE dbo.activitytype (
    id uuid NOT NULL,
    audit_changedby uuid,
    audit_changedbysystem uuid,
    audit_changeoperation text,
    audit_validfrom timestamp with time zone NOT NULL,
    type integer NOT NULL,
    subtype integer,
    trigger integer NOT NULL,
    status integer,
    name text NOT NULL,
    description text NOT NULL,
    CONSTRAINT pk_activitytype PRIMARY KEY (id)
);

CREATE OR REPLACE FUNCTION dbo.audit_activitytype_insert_fn() returns TRIGGER language plpgsql AS $$
BEGIN
DECLARE
changed_by UUID;
changed_by_system UUID;
change_operation_id text;
BEGIN
SELECT current_setting('app.changed_by', false) INTO changed_by;
SELECT current_setting('app.changed_by_system', false) INTO changed_by_system;
SELECT current_setting('app.change_operation_id', false) INTO change_operation_id;
IF NEW.audit_changedby IS NULL THEN NEW.audit_changedby := changed_by; END IF;
IF NEW.audit_changedbysystem IS NULL THEN NEW.audit_changedbysystem := changed_by_system; END IF;
IF NEW.audit_changeoperation IS NULL THEN NEW.audit_changeoperation := change_operation_id; END IF;
IF NEW.audit_validfrom IS NULL THEN NEW.audit_validfrom := now(); END IF;
RETURN NEW;
END;
END;
$$;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_activitytype_insert_trg' AND t.tgrelid = to_regclass('dbo.activitytype')) THEN
CREATE OR REPLACE TRIGGER audit_activitytype_insert_trg BEFORE INSERT OR UPDATE ON dbo.activitytype
FOR EACH ROW EXECUTE FUNCTION dbo.audit_activitytype_insert_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_activitytype_update_fn()
RETURNS TRIGGER AS $$
BEGIN
INSERT INTO dbo_history.auditactivitytype (
description,id,name,status,subtype,trigger,type,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.description,OLD.id,OLD.name,OLD.status,OLD.subtype,OLD.trigger,OLD.type,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_activitytype_update_trg' AND t.tgrelid = to_regclass('dbo.activitytype')) THEN
CREATE OR REPLACE TRIGGER audit_activitytype_update_trg AFTER UPDATE ON dbo.activitytype
FOR EACH ROW EXECUTE FUNCTION dbo.audit_activitytype_update_fn();
END IF; END $$;


CREATE OR REPLACE FUNCTION dbo.audit_activitytype_delete_fn()
RETURNS TRIGGER AS $$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditactivitytype (
description,id,name,status,subtype,trigger,type,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.description,OLD.id,OLD.name,OLD.status,OLD.subtype,OLD.trigger,OLD.type,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$ LANGUAGE plpgsql;
DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_activitytype_delete_trg' AND t.tgrelid = to_regclass('dbo.activitytype')) THEN
CREATE OR REPLACE TRIGGER audit_activitytype_delete_trg AFTER DELETE ON dbo.activitytype
FOR EACH ROW EXECUTE FUNCTION dbo.audit_activitytype_delete_fn();
END IF; END $$;


GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.activitytype TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo.activitytype TO platform_authorization_admin;


CREATE TABLE dbo_history.auditactivitytype (
    audit_validfrom timestamp with time zone NOT NULL,
    id uuid NOT NULL,
    audit_validto timestamp with time zone NOT NULL,
    audit_deletedby uuid,
    audit_deletedbysystem uuid,
    audit_deleteoperation text,
    audit_changedby uuid,
    audit_changedbysystem uuid,
    audit_changeoperation text,
    type integer NOT NULL,
    subtype integer,
    trigger integer NOT NULL,
    status integer,
    name text,
    description text,
    CONSTRAINT pk_auditactivitytype PRIMARY KEY (id, audit_validfrom, audit_validto)
);

GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo_history.auditactivitytype TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES ON TABLE dbo_history.auditactivitytype TO platform_authorization_admin;


CREATE UNIQUE INDEX ix_activitytype_type_subtype_trigger_status ON dbo.activitytype (type, subtype, trigger, status) NULLS NOT DISTINCT;


COMMIT;

