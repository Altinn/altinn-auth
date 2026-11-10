ALTER TABLE resourceregistry.access_list_state
-- version column used for optimistic concurrency control
    ADD COLUMN IF NOT EXISTS version bigint;

UPDATE
    resourceregistry.access_list_state
SET
    version =(
        SELECT
            MAX(eid)
        FROM
            resourceregistry.access_list_events
        WHERE
            resourceregistry.access_list_events.aggregate_id = resourceregistry.access_list_state.aggregate_id)
WHERE
    version IS NULL;

ALTER TABLE resourceregistry.access_list_state
    ALTER COLUMN version SET NOT NULL;

