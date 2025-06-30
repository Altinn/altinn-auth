-- Enum: consent.event_type

DO $$ BEGIN
	CREATE TYPE consent.event_type AS ENUM ('accepted', 'rejected', 'deleted', 'created', 'revoked');
EXCEPTION
	WHEN duplicate_object THEN null;
END $$;
