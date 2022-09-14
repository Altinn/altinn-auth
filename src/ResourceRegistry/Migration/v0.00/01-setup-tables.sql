-- Table: resourceregistry.resources
CREATE TABLE IF NOT EXISTS resourceregistry.resources
(
    identifier text COLLATE pg_catalog."default" NOT NULL,
    created time with time zone NOT NULL,
    modified time with time zone,
    serviceresourcejson jsonb,
    CONSTRAINT resourceregistry_pkey PRIMARY KEY (identifier)
)

TABLESPACE pg_default;