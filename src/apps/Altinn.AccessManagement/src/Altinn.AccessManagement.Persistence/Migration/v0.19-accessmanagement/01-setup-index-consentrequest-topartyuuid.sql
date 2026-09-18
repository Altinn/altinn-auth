-- Index for consent

-- consent.consentrequest: party lookup by recipient (toPartyUuid).
-- Backs GetConsentEventsForParty, which filters consentrequest on topartyuuid before
-- joining consentevent. Without this index the planner does a full seq scan of
-- consentrequest, discarding all non-matching rows on every call.
-- Kept only as the temporary read path (materialized-CTE query) while consentevent.topartyuuid is
-- backfilled. Once the join-free query ships against idx_consentevent_topartyuuid_feed, this index
-- is no longer used and can be dropped.
CREATE INDEX IF NOT EXISTS idx_consentrequest_topartyuuid_incl ON consent.consentrequest USING btree (topartyuuid) include (consentrequestid);
CREATE INDEX IF NOT EXISTS idx_consentevent_event_feed_incl ON consent.consentevent USING btree (consentrequestid, consenteventid) include (eventtype, created);
