-- The consent schema is now provisioned by EF Core
-- (ConsentSchema_Baseline in Altinn.AccessMgmt.PersistenceEF).
-- This version is intentionally a no-op: it is kept so the migration version
-- sequence is preserved for databases already migrated to this version by the
-- earlier pipeline. Future consent DDL must ship as an EF migration, not here.
SELECT 1;
