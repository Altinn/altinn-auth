-- Procedure: get_resource
CREATE OR REPLACE FUNCTION resourceregistry.get_resource(
	_identifier character varying)
    RETURNS resourceregistry.resources
    LANGUAGE 'sql'
    COST 100
    STABLE PARALLEL SAFE 
AS $BODY$
SELECT identifier, created, modified, serviceresourcejson
	FROM resourceregistry.resources
	WHERE identifier = _identifier
$BODY$;

-- Procedure: delete_resource
CREATE OR REPLACE FUNCTION resourceregistry.delete_resource(
	_identifier text)
    RETURNS resourceregistry.resources
    LANGUAGE 'sql'
    COST 100
    VOLATILE PARALLEL SAFE 
AS $BODY$
DELETE FROM resourceregistry.resources
	WHERE identifier = _identifier
	RETURNING *;
$BODY$;

-- Procedure: create_resource
CREATE OR REPLACE FUNCTION resourceregistry.create_resource(
	_identifier text,
	_serviceresourcejson jsonb)
    RETURNS resourceregistry.resources
    LANGUAGE 'sql'
    COST 100
    VOLATILE PARALLEL SAFE 
AS $BODY$
INSERT INTO resourceregistry.resources(
	identifier,
	created,
	modified,
	serviceresourcejson
)
VALUES (
	_identifier,
	Now(),
	Now(),
	_serviceresourcejson
)
RETURNING *;
$BODY$;

-- Procedure: search_for_resource
CREATE OR REPLACE FUNCTION resourceregistry.search_for_resource(
	_id text,
	_title text,
	_description text,
	_resourcetype resourceregistry.resourcetype,
	_keyword text,
	_includeexpired boolean)
    RETURNS SETOF resourceregistry.resources 
    LANGUAGE 'sql'
    COST 100
    STABLE PARALLEL SAFE 
    ROWS 1000

AS $BODY$
SELECT identifier, created, modified, serviceresourcejson
	FROM resourceregistry.resources
	WHERE (_id IS NULL OR serviceresourcejson ->> 'identifier' iLike concat('%', _id, '%'))
	AND (_title IS NULL OR serviceresourcejson ->> 'title' iLike concat('%', _title, '%'))
	AND (_description IS NULL OR serviceresourcejson ->> 'description' iLike concat('%', _description, '%'))
	AND (_resourcetype IS NULL OR serviceresourcejson ->> 'resourceType' iLike _resourcetype::text)
	AND (_keyword IS NULL OR serviceresourcejson ->> 'keywords' iLike concat('%', _keyword, '%'))
	AND (_includeexpired IS true OR (serviceresourcejson ->> 'validTo')::timestamptz > 'now'::timestamptz
		AND (serviceresourcejson ->> 'validFrom')::timestamptz < 'now'::timestamptz)
$BODY$;

-- Procedure: update_resource
CREATE OR REPLACE FUNCTION resourceregistry.update_resource(
	_identifier text,
	_serviceresourcejson jsonb)
    RETURNS resourceregistry.resources
    LANGUAGE 'sql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
UPDATE resourceregistry.resources
SET
	modified = Now(),
	serviceresourcejson = _serviceresourcejson
WHERE 
identifier = _identifier
RETURNING *;
$BODY$;