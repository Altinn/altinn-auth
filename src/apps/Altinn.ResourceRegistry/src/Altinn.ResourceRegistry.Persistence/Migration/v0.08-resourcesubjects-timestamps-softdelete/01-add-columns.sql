ALTER TABLE resourceregistry.resourcesubjects
    ADD COLUMN updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    ADD COLUMN deleted BOOLEAN DEFAULT FALSE;

CREATE INDEX ix_resourceregistry_resourcesubjects_updatedat
    ON resourceregistry.resourcesubjects (updated_at);

CREATE INDEX ix_resourceregistry_resourcesubjects_deleted
    ON resourceregistry.resourcesubjects (deleted);
