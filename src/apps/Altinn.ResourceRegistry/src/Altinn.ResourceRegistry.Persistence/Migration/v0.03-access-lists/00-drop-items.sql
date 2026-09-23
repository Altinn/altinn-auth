-- Schema: resourceregistry
-- Table: resourceregistry.party_registry_events
DROP INDEX IF EXISTS ix_party_registry_events_rid;

DROP TABLE IF EXISTS resourceregistry.party_registry_events;

-- State Table: resourceregistry.party_registry_members_state
DROP INDEX IF EXISTS ix_party_registry_members_state_rid;

DROP TABLE IF EXISTS resourceregistry.party_registry_members_state;

-- State Table: resourceregistry.party_registry_resource_connections_state
DROP INDEX IF EXISTS ix_party_registry_resource_connections_state_resource;

DROP INDEX IF EXISTS ix_party_registry_resource_connections_state_rid;

DROP TABLE IF EXISTS resourceregistry.party_registry_resource_connections_state;

-- State Table: resourceregistry.party_registry_state
DROP INDEX IF EXISTS uq_party_registry_state_owner_ident;

DROP TABLE IF EXISTS resourceregistry.party_registry_state;

-- Enum: resourceregistry.party_registry_event_kind
DROP TYPE IF EXISTS resourceregistry.party_registry_event_kind;

