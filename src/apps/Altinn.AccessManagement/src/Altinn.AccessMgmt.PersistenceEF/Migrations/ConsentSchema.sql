-- Baseline DDL for the consent schema: the enum types, the consentrequest /
-- consentright / consentevent / context / metadata / resourceattribute tables,
-- their primary keys and indexes, and the grants to platform_authorization.
-- Generated from 'pg_dump --schema-only --schema=consent' of the established
-- database, so it reproduces the schema exactly, then made idempotent (IF NOT
-- EXISTS / guarded CREATE) so it is a no-op on databases that already have the
-- schema and creates it on fresh ones. The consentrequest table uses the
-- public.hstore type, so the hstore extension is ensured up front (pg_dump
-- --schema does not emit extensions).
SET client_min_messages = warning;

CREATE SCHEMA IF NOT EXISTS consent;
CREATE EXTENSION IF NOT EXISTS hstore WITH SCHEMA public;

-- Enum types.
DO $$ BEGIN
    CREATE TYPE consent.event_type AS ENUM ('accepted', 'rejected', 'deleted', 'created', 'revoked', 'used');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE consent.portal_view_mode AS ENUM ('hide', 'show');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE consent.status_type AS ENUM ('unopened', 'opened', 'accepted', 'rejected', 'deleted', 'created', 'revoked');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- Tables.
CREATE TABLE IF NOT EXISTS consent.consentevent (
    consenteventid uuid NOT NULL,
    consentrequestid uuid NOT NULL,
    eventtype consent.event_type NOT NULL,
    created timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    performedbyparty uuid NOT NULL
);

CREATE TABLE IF NOT EXISTS consent.consentrequest (
    consentrequestid uuid NOT NULL,
    frompartyuuid uuid,
    requireddelegatoruuid uuid,
    topartyuuid uuid,
    handledbypartyuuid uuid,
    requestmessage public.hstore,
    redirecturl text,
    isdeleted boolean DEFAULT false,
    created timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    templateid text NOT NULL,
    templateversion integer,
    status consent.status_type DEFAULT 'created'::consent.status_type NOT NULL,
    validto timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    consented timestamp with time zone,
    revoked timestamp with time zone,
    rejected timestamp with time zone,
    portalviewmode consent.portal_view_mode DEFAULT 'hide'::consent.portal_view_mode NOT NULL
);

CREATE TABLE IF NOT EXISTS consent.consentright (
    consentrightid uuid NOT NULL,
    consentrequestid uuid NOT NULL,
    action text[]
);

CREATE TABLE IF NOT EXISTS consent.context (
    contextid uuid NOT NULL,
    consentrequestid uuid NOT NULL,
    language text NOT NULL
);

CREATE TABLE IF NOT EXISTS consent.metadata (
    consentrightid uuid NOT NULL,
    id text,
    value text
);

CREATE TABLE IF NOT EXISTS consent.resourceattribute (
    consentrightid uuid NOT NULL,
    type text,
    value text,
    version text
);

-- Primary keys.
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'consentevent_pkey' AND connamespace = 'consent'::regnamespace) THEN
        ALTER TABLE ONLY consent.consentevent ADD CONSTRAINT consentevent_pkey PRIMARY KEY (consenteventid);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'consentrequest_pkey' AND connamespace = 'consent'::regnamespace) THEN
        ALTER TABLE ONLY consent.consentrequest ADD CONSTRAINT consentrequest_pkey PRIMARY KEY (consentrequestid);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'consentright_pkey' AND connamespace = 'consent'::regnamespace) THEN
        ALTER TABLE ONLY consent.consentright ADD CONSTRAINT consentright_pkey PRIMARY KEY (consentrightid);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'context_pkey' AND connamespace = 'consent'::regnamespace) THEN
        ALTER TABLE ONLY consent.context ADD CONSTRAINT context_pkey PRIMARY KEY (contextid);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'resourceattribute_pkey' AND connamespace = 'consent'::regnamespace) THEN
        ALTER TABLE ONLY consent.resourceattribute ADD CONSTRAINT resourceattribute_pkey PRIMARY KEY (consentrightid);
    END IF;
END $$;

-- Indexes.
CREATE INDEX IF NOT EXISTS idx_consentevent_consentrequestid_created ON consent.consentevent USING btree (consentrequestid, created);
CREATE INDEX IF NOT EXISTS idx_consentrequest_frompartyuuid ON consent.consentrequest USING btree (frompartyuuid);
CREATE INDEX IF NOT EXISTS idx_consentrequest_frompartyuuid_status_portal_show ON consent.consentrequest USING btree (frompartyuuid, status) WHERE (portalviewmode = 'show'::consent.portal_view_mode);
CREATE INDEX IF NOT EXISTS idx_consentrequest_status ON consent.consentrequest USING btree (status);
CREATE INDEX IF NOT EXISTS idx_consentright_consentrequestid ON consent.consentright USING btree (consentrequestid);
CREATE INDEX IF NOT EXISTS idx_context_consentrequestid ON consent.context USING btree (consentrequestid);
CREATE INDEX IF NOT EXISTS idx_metadata_consentrightid ON consent.metadata USING btree (consentrightid);

-- Grants.
GRANT USAGE ON SCHEMA consent TO platform_authorization;
GRANT ALL ON TABLE consent.consentevent TO platform_authorization;
GRANT ALL ON TABLE consent.consentrequest TO platform_authorization;
GRANT ALL ON TABLE consent.consentright TO platform_authorization;
GRANT ALL ON TABLE consent.context TO platform_authorization;
GRANT ALL ON TABLE consent.metadata TO platform_authorization;
GRANT ALL ON TABLE consent.resourceattribute TO platform_authorization;
-- Mirrors the original Yuniql grants. The consent schema has no sequences today
-- (all UUID PKs), so this is a no-op now; kept for parity and so any sequence later
-- added to the schema is granted on (re)provisioning.
GRANT ALL ON ALL SEQUENCES IN SCHEMA consent TO platform_authorization;
