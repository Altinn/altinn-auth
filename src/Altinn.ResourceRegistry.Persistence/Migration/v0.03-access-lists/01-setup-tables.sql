-- Schema: resourceregistry
-- Enum: resourceregistry.access_list_event_kind
DO $$
BEGIN
    CREATE TYPE resourceregistry.access_list_event_kind AS ENUM(
        'created',
        'updated',
        'deleted',
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

-- Table: resourceregistry.access_list_events
CREATE TABLE IF NOT EXISTS resourceregistry.access_list_events(
    -- event id
    eid bigint NOT NULL GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    -- event time
    etime timestamp with time zone NOT NULL,
    -- event kind
    kind resourceregistry.access_list_event_kind NOT NULL,
    -- event target (aggregate id)
    aggregate_id uuid NOT NULL,
    -- identifier - created.access_list_identifier, resource_connection_*.resource_identifier
    identifier text COLLATE pg_catalog."default",
    -- name - created.name, updated.name -- display name for an access list
    name text COLLATE pg_catalog."default",
    -- description - created.description, updated.description -- description for an access list
    description text COLLATE pg_catalog."default",
    -- resource_owner - created.resource_owner -- the org. nr of the owning party
    resource_owner text COLLATE pg_catalog."default",
    -- actions - resource_connection_created.actions, resource_connection_actions_added.actions, resource_connection_actions_removed.actions
    actions text[] COLLATE pg_catalog."default",
    -- party_id - members_added.party_ids, members_removed.party_ids
    party_ids uuid[]
)
TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS ix_access_list_events_aggregate_id ON resourceregistry.access_list_events(aggregate_id) TABLESPACE pg_default;

-- State Table: resourceregistry.access_list_state
CREATE TABLE IF NOT EXISTS resourceregistry.access_list_state(
    -- access list id
    aggregate_id uuid NOT NULL PRIMARY KEY,
    -- identifier - public "id" (changeable) - unique per owner
    identifier text COLLATE pg_catalog."default" NOT NULL,
    -- resource_owner - the org. nr of the owning party
    resource_owner text COLLATE pg_catalog."default" NOT NULL,
    -- name of the access list (display name)
    name text COLLATE pg_catalog."default" NOT NULL,
    -- description of the access list
    description text COLLATE pg_catalog."default" NOT NULL,
    -- when the access list was created
    created timestamp with time zone NOT NULL,
    -- last update time for the access list
    modified timestamp with time zone NOT NULL
)
TABLESPACE pg_default;

CREATE UNIQUE INDEX IF NOT EXISTS uq_access_list_state_owner_ident ON resourceregistry.access_list_state(resource_owner, identifier) TABLESPACE pg_default;

-- State Table: resourceregistry.access_list_members_state
CREATE TABLE IF NOT EXISTS resourceregistry.access_list_members_state(
    -- access list id
    aggregate_id uuid NOT NULL,
    -- party id
    party_id uuid NOT NULL,
    -- since when this member was added
    since timestamp with time zone NOT NULL,
    PRIMARY KEY (aggregate_id, party_id),
    FOREIGN KEY (aggregate_id) REFERENCES resourceregistry.access_list_state(aggregate_id) ON DELETE CASCADE ON UPDATE RESTRICT
)
TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS ix_access_list_members_state_aggregate_id ON resourceregistry.access_list_members_state(aggregate_id) TABLESPACE pg_default;

-- State Table: resourceregistry.access_list_resource_connections_state
CREATE TABLE IF NOT EXISTS resourceregistry.access_list_resource_connections_state(
    -- access list id
    aggregate_id uuid NOT NULL,
    -- resource id
    resource_identifier text COLLATE pg_catalog."default" NOT NULL,
    -- actions
    actions text[] COLLATE pg_catalog."default" NOT NULL,
    -- when the connection was created
    created timestamp with time zone NOT NULL,
    -- last update time for the connection
    modified timestamp with time zone NOT NULL,
    PRIMARY KEY (aggregate_id, resource_identifier),
    FOREIGN KEY (resource_identifier) REFERENCES resourceregistry.resources(identifier) ON DELETE RESTRICT ON UPDATE RESTRICT,
    FOREIGN KEY (aggregate_id) REFERENCES resourceregistry.access_list_state(aggregate_id) ON DELETE CASCADE ON UPDATE RESTRICT
)
TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS ix_access_list_resource_connections_state_resource ON resourceregistry.access_list_resource_connections_state(resource_identifier) TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS ix_access_list_resource_connections_state_aggregate_id ON resourceregistry.access_list_resource_connections_state(aggregate_id) TABLESPACE pg_default;

