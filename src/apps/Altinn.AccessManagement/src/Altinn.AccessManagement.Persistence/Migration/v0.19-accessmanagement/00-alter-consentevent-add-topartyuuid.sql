-- consent.consentevent: denormalized recipient party.
-- topartyuuid is copied from the parent consentrequest so the event feed
-- (GetConsentEventsForParty) can filter + order + paginate on consentevent alone,
-- without joining consentrequest. Adding a nullable column is a metadata-only change
-- (no table rewrite). New rows are populated on insert; existing rows are backfilled
-- operationally, after which the
-- supporting index is built CONCURRENTLY outside this transactional migration.
ALTER TABLE consent.consentevent
    ADD COLUMN IF NOT EXISTS topartyuuid uuid;
