-- Migration script to split resources table and add versioning
-- Step 1: Create the new resource_identifier table
CREATE TABLE resourceregistry.resource_identifier
(
    identifier text PRIMARY KEY,
    created timestamp with time zone NOT NULL
);

-- Step 2: Insert data from existing resources table into resource_identifier
INSERT INTO resourceregistry.resource_identifier (identifier, created)
SELECT identifier, created 
FROM resourceregistry.resources
ORDER BY identifier;

-- Remove the primary key constraint
alter table resourceregistry.access_list_resource_connections_state 
drop constraint access_list_resource_connections_state_resource_identifier_fkey;

-- Step 3: Drop the primary key constraint from resources table
ALTER TABLE resourceregistry.resources 
DROP CONSTRAINT resourceregistry_pkey;

-- Step 4: Add foreign key constraint using existing identifier
ALTER TABLE resourceregistry.resources 
ADD CONSTRAINT fk_resources_resource_identifier 
FOREIGN KEY (identifier) REFERENCES resourceregistry.resource_identifier(identifier);

-- Step 5: Add versionid column with automatic generation
ALTER TABLE resourceregistry.resources 
ADD COLUMN version_id BIGSERIAL NOT NULL PRIMARY KEY;

-- Step 6: Create indexes for better performance
CREATE INDEX idx_resources_identifier ON resourceregistry.resources(identifier);
create index idx_resources_lookup on resourceregistry.resources(identifier, version_id desc);

CREATE VIEW resourceregistry.current_resources as
select distinct on (res.identifier) res.*
from resourceregistry.resources res
order by res.identifier, res.version_id desc;
