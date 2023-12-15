DROP FUNCTION IF EXISTS resourceregistry.create_resource(text, jsonb);
DROP FUNCTION IF EXISTS resourceregistry.delete_resource(text);
DROP FUNCTION IF EXISTS resourceregistry.get_resource(character varying);
DROP FUNCTION IF EXISTS resourceregistry.search_for_resource(text, text, text, resourceregistry.resourcetype, text, boolean);
DROP FUNCTION IF EXISTS resourceregistry.search_for_resource(text, text, text, resourceregistry.resourcetype, text);
DROP FUNCTION IF EXISTS resourceregistry.update_resource(text, jsonb);
