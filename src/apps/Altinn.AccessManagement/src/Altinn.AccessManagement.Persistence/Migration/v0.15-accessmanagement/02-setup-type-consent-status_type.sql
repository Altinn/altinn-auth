-- Enum: consent.status_type

DO $$ BEGIN
	CREATE TYPE consent.status_type AS ENUM ('unopened', 'opened', 'accepted', 'rejected', 'deleted', 'created', 'revoked');
EXCEPTION
	WHEN duplicate_object THEN null;
END $$;
