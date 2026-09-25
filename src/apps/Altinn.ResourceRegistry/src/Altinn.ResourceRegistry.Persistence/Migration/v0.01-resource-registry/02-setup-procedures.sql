-- Procedure: search_for_resource
CREATE OR REPLACE FUNCTION resourceregistry.search_for_resource(
	_id text,
	_title text,
	_description text,
	_resourcetype resourceregistry.resourcetype,
	_keyword text)
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
$BODY$;
