-- Support for the resource change feed. Each resource gets a global_change_id assigned from a
-- global sequence whenever it changes (metadata create/update, policy upload or delete), and
-- the feed lists resources ordered by this value. One row per resource means each resource
-- appears in the feed at most once, at its latest change.

CREATE SEQUENCE resourceregistry.resource_change_id_seq AS bigint;

ALTER TABLE resourceregistry.resource_identifier
    ADD COLUMN global_change_id bigint,
    ADD COLUMN last_changed timestamp with time zone,
    ADD COLUMN policy_uploaded boolean NOT NULL DEFAULT true,
    ADD COLUMN deleted boolean NOT NULL DEFAULT false;

-- policy_uploaded tells that a policy has been uploaded at least once for the resource. Only
-- flagged resources are listed in the resource change feed. A policy may be empty on purpose,
-- so this is tracked as an explicit flag rather than derived from resourcesubjects.
-- The column is added with DEFAULT true so all existing resources are flagged (existing
-- resources are assumed to have a policy; the policy documents live in blob storage and cannot
-- be checked from a migration), then the default is flipped so future resources start
-- unflagged until their first policy upload.
ALTER TABLE resourceregistry.resource_identifier
    ALTER COLUMN policy_uploaded SET DEFAULT false;

-- Backfill: assign change ids to existing resources in the order of their latest version, so
-- the initial feed ordering matches the historical change ordering.
WITH latest AS (
    SELECT DISTINCT ON (identifier) identifier, COALESCE(modified, created) AS changed_at, version_id
    FROM resourceregistry.resources
    ORDER BY identifier, version_id DESC
), ordered AS (
    SELECT identifier, changed_at, nextval('resourceregistry.resource_change_id_seq') AS change_id
    FROM (SELECT * FROM latest ORDER BY version_id) versions
)
UPDATE resourceregistry.resource_identifier ri
SET global_change_id = ordered.change_id,
    last_changed = ordered.changed_at
FROM ordered
WHERE ri.identifier = ordered.identifier;

-- Identifier rows without any version rows belong to resources that have been deleted
-- (deletes remove the version rows but keep the identifier row).
UPDATE resourceregistry.resource_identifier ri
SET global_change_id = nextval('resourceregistry.resource_change_id_seq'),
    last_changed = now(),
    deleted = true
WHERE ri.global_change_id IS NULL;

-- New identifier rows draw their change id through tx_nextval so in-flight values are visible
-- to tx_max_safeval; see 01-functions.sql.
ALTER TABLE resourceregistry.resource_identifier
    ALTER COLUMN global_change_id SET NOT NULL,
    ALTER COLUMN global_change_id SET DEFAULT resourceregistry.tx_nextval('resourceregistry.resource_change_id_seq'),
    ALTER COLUMN last_changed SET NOT NULL,
    ALTER COLUMN last_changed SET DEFAULT now();

CREATE UNIQUE INDEX uq_resource_identifier_global_change_id
    ON resourceregistry.resource_identifier (global_change_id);
