-- Table: consent.consentrequest

DO $$ BEGIN
	CREATE TYPE consent.portal_view_mode AS ENUM ('hide', 'show');
EXCEPTION
	WHEN duplicate_object THEN null;
END $$;
