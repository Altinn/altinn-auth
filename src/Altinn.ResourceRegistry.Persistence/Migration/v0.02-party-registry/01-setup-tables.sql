-- Schema: resourceregistry
-- Enum: resourceregistry.party_registry_event_kind
DO $$
BEGIN
    CREATE TYPE resourceregistry.party_registry_event_kind AS ENUM(
        'registry_created',
        'registry_updated',
        'registry_deleted',
        'resource_connection_created',
        'resource_connection_actions_added',
        'resource_connection_actions_removed',
        'resource_connection_deleted',
        'members_added',
        'members_removed'
);
EXCEPTION
    WHEN duplicate_object THEN
        NULL;
END
$$;

-- Table: resourceregistry.party_registry_events
CREATE TABLE IF NOT EXISTS resourceregistry.party_registry_events(
    -- event id
    eid bigint NOT NULL GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    -- event time
    etime timestamp with time zone NOT NULL,
    -- event kind
    kind resourceregistry.party_registry_event_kind NOT NULL,
    -- event target (registry id)
    rid uuid NOT NULL,
    -- identifier - registry_created.registry_identifier, resource_connection_*.resource_identifier
    identifier text COLLATE pg_catalog."default",
    -- registry_name - registry_created.registry_name, registry_updated.registry_name -- display name for a registry
    registry_name text COLLATE pg_catalog."default",
    -- registry_description - registry_created.registry_description, registry_updated.registry_description -- description for a registry
    registry_description text COLLATE pg_catalog."default",
    -- registry_owner - registry_created.registry_owner -- the org. nr of the owning party
    registry_owner text COLLATE pg_catalog."default",
    -- actions - resource_connection_created.actions, resource_connection_actions_added.actions, resource_connection_actions_removed.actions
    actions text[] COLLATE pg_catalog."default",
    -- party_id - members_added.party_ids, members_removed.party_ids
    party_ids uuid[]
)
TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS ix_party_registry_events_rid ON resourceregistry.party_registry_events(rid) TABLESPACE pg_default;

-- State Table: resourceregistry.party_registry_state
CREATE TABLE IF NOT EXISTS resourceregistry.party_registry_state(
    -- registry id
    rid uuid NOT NULL PRIMARY KEY,
    -- identifier - public "id" (changeable) - unique per owner
    identifier text COLLATE pg_catalog."default" NOT NULL,
    -- registry_owner - the org. nr of the owning party
    registry_owner text COLLATE pg_catalog."default" NOT NULL,
    -- name of the registry (display name)
    registry_name text COLLATE pg_catalog."default" NOT NULL,
    -- description of the registry
    registry_description text COLLATE pg_catalog."default" NOT NULL,
    -- when the registry was created
    created timestamp with time zone NOT NULL,
    -- last update time for the registry
    modified timestamp with time zone NOT NULL
)
TABLESPACE pg_default;

CREATE UNIQUE INDEX IF NOT EXISTS uq_party_registry_state_owner_ident ON resourceregistry.party_registry_state(registry_owner, identifier) TABLESPACE pg_default;

-- State Table: resourceregistry.party_registry_members_state
CREATE TABLE IF NOT EXISTS resourceregistry.party_registry_members_state(
    -- registry id
    rid uuid NOT NULL,
    -- party id
    party_id uuid NOT NULL,
    -- since when this member was added
    since timestamp with time zone NOT NULL,
    PRIMARY KEY (rid, party_id),
    FOREIGN KEY (rid) REFERENCES resourceregistry.party_registry_state(rid) ON DELETE CASCADE ON UPDATE RESTRICT
)
TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS ix_party_registry_members_state_rid ON resourceregistry.party_registry_members_state(rid) TABLESPACE pg_default;

-- State Table: resourceregistry.party_registry_resource_connections_state
CREATE TABLE IF NOT EXISTS resourceregistry.party_registry_resource_connections_state(
    -- registry id
    rid uuid NOT NULL,
    -- resource id
    resource_identifier text COLLATE pg_catalog."default" NOT NULL,
    -- actions
    actions text[] COLLATE pg_catalog."default" NOT NULL,
    -- when the connection was created
    created timestamp with time zone NOT NULL,
    -- last update time for the connection
    modified timestamp with time zone NOT NULL,
    PRIMARY KEY (rid, resource_identifier),
    FOREIGN KEY (resource_identifier) REFERENCES resourceregistry.resources(identifier) ON DELETE RESTRICT ON UPDATE RESTRICT,
    FOREIGN KEY (rid) REFERENCES resourceregistry.party_registry_state(rid) ON DELETE CASCADE ON UPDATE RESTRICT
)
TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS ix_party_registry_resource_connections_state_resource ON resourceregistry.party_registry_resource_connections_state(resource_identifier) TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS ix_party_registry_resource_connections_state_rid ON resourceregistry.party_registry_resource_connections_state(rid) TABLESPACE pg_default;

-- Table: resourceregistry.resources - make serviceresourcejson NOT NULL
ALTER TABLE resourceregistry.resources
    ALTER COLUMN serviceresourcejson SET NOT NULL;

