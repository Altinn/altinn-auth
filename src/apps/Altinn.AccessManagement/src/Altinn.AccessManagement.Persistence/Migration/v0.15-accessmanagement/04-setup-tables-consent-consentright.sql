-- Table: consent.consentright

CREATE TABLE IF NOT EXISTS consent.consentright
(
    consentRightId uuid PRIMARY KEY NOT NULL,
    consentRequestId uuid NOT NULL,
    action text[]
);




