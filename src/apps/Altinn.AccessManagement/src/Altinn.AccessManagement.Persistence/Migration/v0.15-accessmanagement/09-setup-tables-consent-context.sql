-- Table: consent.context

CREATE TABLE IF NOT EXISTS consent.context
(
  contextId uuid PRIMARY KEY NOT NULL,
  consentRequestId uuid NOT NULL,
  language text NOT NULL
);



