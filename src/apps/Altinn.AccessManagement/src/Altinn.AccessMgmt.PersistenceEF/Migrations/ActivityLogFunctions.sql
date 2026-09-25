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
