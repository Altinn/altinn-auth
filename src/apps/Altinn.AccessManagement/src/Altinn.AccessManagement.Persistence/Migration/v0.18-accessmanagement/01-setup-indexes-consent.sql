-- Index for consent

-- consent.consentrequest: party lookup for general queries (e.g. GetRequestsForParty)
CREATE INDEX IF NOT EXISTS idx_consentrequest_frompartyuuid
    ON consent.consentrequest (fromPartyUuid);

-- consent.consentrequest: party + status for portal-visible consent requests (count endpoint)
CREATE INDEX IF NOT EXISTS idx_consentrequest_frompartyuuid_status_portal_show
    ON consent.consentrequest (fromPartyUuid, status)
    WHERE portalviewmode = 'show';

-- consent.consentrequest: status-only lookup for getting consent requests by status
CREATE INDEX IF NOT EXISTS idx_consentrequest_status
    ON consent.consentrequest (status);

-- consent.consentright: FK lookup used by multiple queries
CREATE INDEX IF NOT EXISTS idx_consentright_consentrequestid
    ON consent.consentright (consentRequestId);

-- consent.metadata: JOIN column (no existing index)
CREATE INDEX IF NOT EXISTS idx_metadata_consentrightid
    ON consent.metadata (consentRightId);

-- consent.consentevent: filter + sort for GetEvents()
CREATE INDEX IF NOT EXISTS idx_consentevent_consentrequestid_created
    ON consent.consentevent (consentRequestId, created);

-- consent.context: FK lookup for GetConsentContext()
CREATE INDEX IF NOT EXISTS idx_context_consentrequestid
    ON consent.context (consentRequestId);

