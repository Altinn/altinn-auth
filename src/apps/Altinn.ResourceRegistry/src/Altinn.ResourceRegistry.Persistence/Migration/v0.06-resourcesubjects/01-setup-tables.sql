-- Schema: resourceregistry

-- Table: resourceregistry.resourcesubjects
CREATE TABLE IF NOT EXISTS resourceregistry.resourcesubjects(
    -- resource_type - urn:altinn:resource ++
    resource_type text COLLATE pg_catalog."default",
    -- resource_value - Identifer for resource in resource registry
    resource_value text COLLATE pg_catalog."default",
    -- resource_urn - urn of subject. Example urn:altinn:rolecode:utinn ++
    resource_urn text COLLATE pg_catalog."default",
    -- subject_type - Type of subject. Example urn:altinn:rolecode ++
    subject_type text COLLATE pg_catalog."default",
    -- subject_value - Reference to rolecode, accesspackage +++
    subject_value text COLLATE pg_catalog."default",
    -- subject_urn - urn to rolecode, accesspackage. Full URN
    subject_urn text COLLATE pg_catalog."default",
    -- registry_owner - registry_created.registry_owner -- the org. nr of the owning party. Example urn:altinn:org:skd
    resource_owner text COLLATE pg_catalog."default",

    PRIMARY KEY(resource_urn, subject_urn)
)
TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS ix_resourceregistry_resourcesubjects_resourceurn ON resourceregistry.resourcesubjects(resource_urn) TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS ix_resourceregistry_resourcesubjects_subjecturn ON resourceregistry.resourcesubjects(subject_urn) TABLESPACE pg_default;

