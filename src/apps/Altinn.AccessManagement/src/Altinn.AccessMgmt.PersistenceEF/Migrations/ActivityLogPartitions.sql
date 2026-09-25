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
