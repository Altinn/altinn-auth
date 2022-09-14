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
	_created timestamp with time zone,
	_modified timestamp with time zone,
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
	_created,
	_modified,
	_serviceresourcejson
)
RETURNING *;
$BODY$;

-- Procedure: search_for_resource
CREATE OR REPLACE FUNCTION resourceregistry.search_for_resource(IN _searchterm text)
    RETURNS SETOF resourceregistry.resources
    LANGUAGE 'sql'
    STABLE
    PARALLEL SAFE
    COST 100    ROWS 1000 
    
AS $BODY$
SELECT identifier, created, modified, serviceresourcejson
	FROM resourceregistry.resources
	WHERE serviceresourcejson ->> 'title' iLike concat('%', _searchterm, '%');
$BODY$;