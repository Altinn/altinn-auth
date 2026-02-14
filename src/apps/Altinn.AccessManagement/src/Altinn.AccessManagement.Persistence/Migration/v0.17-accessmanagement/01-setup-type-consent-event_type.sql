-- Enum: consent.event_type

DO $$ BEGIN
	ALTER TYPE consent.event_type ADD VALUE IF NOT EXISTS 'used';
EXCEPTION
	WHEN duplicate_object THEN null;
END $$;
