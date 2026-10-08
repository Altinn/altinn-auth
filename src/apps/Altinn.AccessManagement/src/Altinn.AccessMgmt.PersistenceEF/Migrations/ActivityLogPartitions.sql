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
-- synthesizes events strictly before the cutoff, so it can never duplicate trigger-written
-- rows. clock_timestamp() on purpose: now() is frozen at transaction start, which predates
-- the trigger DDL above — a row committed in between would then be neither trigger-logged
-- nor backfilled. Statement time after the trigger DDL leaves no such gap.
INSERT INTO dbo.activitylogbackfillprogress (source, cutoff)
VALUES
    ('assignment', clock_timestamp()),
    ('assignmentpackage', clock_timestamp()),
    ('assignmentresource', clock_timestamp()),
    ('assignmentinstance', clock_timestamp()),
    ('delegation', clock_timestamp()),
    ('delegationpackage', clock_timestamp()),
    ('delegationresource', clock_timestamp()),
    ('requestassignment', clock_timestamp()),
    ('requestassignmentpackage', clock_timestamp()),
    ('requestassignmentresource', clock_timestamp())
ON CONFLICT (source) DO NOTHING;
