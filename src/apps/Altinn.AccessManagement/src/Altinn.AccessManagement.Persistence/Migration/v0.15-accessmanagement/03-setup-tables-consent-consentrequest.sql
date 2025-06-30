-- Table: consent.consentrequest

CREATE TABLE IF NOT EXISTS consent.consentrequest
(
    consentRequestId uuid PRIMARY KEY NOT NULL,
    fromPartyUuid UUID,
    requiredDelegatorUuid UUID null,
    toPartyUuid UUID,
    handledByPartyUuid UUID null,
    requestMessage hstore null,
    redirecturl text,
    isDeleted bool default False,
    created timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    templateId text NOT NULL,
    templateVersion integer NULL,
    status consent.status_type NOT NULL DEFAULT 'created',
    validto  timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    consented timestamp with time zone NULL,
    revoked timestamp with time zone NULL,
    rejected timestamp with time zone NULL
);


