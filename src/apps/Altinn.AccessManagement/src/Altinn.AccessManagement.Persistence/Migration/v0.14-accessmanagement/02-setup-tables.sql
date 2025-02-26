-- Enum: delegation.instanceType
DO $$ BEGIN
	CREATE TYPE delegation.uuidtype AS ENUM ('urn:altinn:person:uuid', 'urn:altinn:organization:uuid', 'urn:altinn:systemuser:uuid', 'urn:altinn:enterpriseuser:uuid', 'urn:altinn:resource', 'urn:altinn:party:uuid');
EXCEPTION
	WHEN duplicate_object THEN 
        ALTER TYPE delegation.uuidtype ADD VALUE IF NOT EXISTS 'urn:altinn:party:uuid';
END $$;


