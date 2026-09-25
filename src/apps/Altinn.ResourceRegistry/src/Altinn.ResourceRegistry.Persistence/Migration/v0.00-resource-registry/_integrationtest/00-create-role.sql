DO $$ BEGIN
    CREATE ROLE platform_authorization;
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;
