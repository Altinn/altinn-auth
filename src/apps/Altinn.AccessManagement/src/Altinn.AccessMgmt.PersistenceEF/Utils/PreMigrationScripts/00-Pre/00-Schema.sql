CREATE SCHEMA ingest;
ALTER SCHEMA ingest OWNER TO platform_authorization_admin;
GRANT USAGE, CREATE ON SCHEMA ingest TO platform_authorization;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA ingest TO platform_authorization;
