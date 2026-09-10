-- ─────────────────────────────────────────────────────────────────────────────
-- Manual backfill: set ParentId for existing ADOS (administrative unit - public
-- sector) subunits so they inherit mainunit access, equal to BEDR/AAFY.
--
-- Context
--   The RoleSyncService consumes the CCR external-role stream incrementally and
--   only sets Entity.ParentId for ADOS going forward (once the feature flag
--   "AccessManagement.Subunit.AdosInheritance" is enabled). Rows imported before
--   the flag was enabled therefore have a NULL ParentId. This script backfills
--   those rows from the existing ADOS role assignments already present in the DB.
--
--   Mirrors RoleSyncService write-side logic where, for the ADOS registration-unit
--   role, the subunit is the assignment FromId and the mainunit is the ToId
--   (addParent[assignment.FromId] = assignment.ToId).
--
-- Reference identifiers
--   ADOS registration-unit role : 66ad5542-4f4a-4606-996f-18690129ce00  (RoleConstants.AdministrativeUnitPublicSector)
--   ADOS entity variant         : e57cac52-e401-4c0f-a1cf-8bb4628fe671  (EntityVariantConstants.ADOS)
--   RegisterImportSystem entity : efec83fc-deba-4f09-8073-b4dd19d0b16b  (SystemEntityConstants.RegisterImportSystem)
--
-- Execution
--   Run manually AFTER enabling the AccessManagement.Subunit.AdosInheritance flag.
--   Idempotent: only touches rows whose ParentId differs from the expected value,
--   so it can be re-run safely.
--
--   The audit triggers on dbo.entity (audit_entity_insert_trg / audit_entity_update
--   / audit_entity_delete) require an audit context. We mimic the register import
--   system as the actor, matching AuditValues(SystemEntityConstants.RegisterImportSystem),
--   by setting both the SET LOCAL app.* settings (read by audit_entity_insert_fn)
--   and the session_audit_context temp table (read by the delete trigger).
-- ─────────────────────────────────────────────────────────────────────────────

BEGIN;

-- Audit context: attribute the change to the RegisterImportSystem, as if the
-- change happened through the normal register import pipeline.
CREATE TEMP TABLE IF NOT EXISTS session_audit_context (
		changed_by          UUID,
		changed_by_system   UUID,
		change_operation_id TEXT
) ON COMMIT DROP;
TRUNCATE session_audit_context;
INSERT INTO session_audit_context (changed_by, changed_by_system, change_operation_id)
VALUES (
		'efec83fc-deba-4f09-8073-b4dd19d0b16b',
		'efec83fc-deba-4f09-8073-b4dd19d0b16b',
		'backfill-ados-parentid'
);

SET LOCAL app.changed_by          = 'efec83fc-deba-4f09-8073-b4dd19d0b16b';
SET LOCAL app.changed_by_system   = 'efec83fc-deba-4f09-8073-b4dd19d0b16b';
SET LOCAL app.change_operation_id = 'backfill-ados-parentid';

-- Backfill ParentId (and audit columns) for ADOS subunits that are missing it.
-- audit_* is set explicitly because the insert/update trigger only fills these
-- columns when they are NULL; on UPDATE the existing (non-null) values would
-- otherwise be retained instead of attributing the change to the import system.
UPDATE dbo.entity AS sub
SET    parentid                = a.toid,
			 audit_changedby         = 'efec83fc-deba-4f09-8073-b4dd19d0b16b',
			 audit_changedbysystem   = 'efec83fc-deba-4f09-8073-b4dd19d0b16b',
			 audit_changeoperation   = 'backfill-ados-parentid',
			 audit_validfrom         = now()
FROM   dbo.assignment AS a
WHERE  a.fromid    = sub.id
	AND  a.roleid    = '66ad5542-4f4a-4606-996f-18690129ce00'   -- ADOS registration-unit role
	AND  sub.variantid = 'e57cac52-e401-4c0f-a1cf-8bb4628fe671' -- ADOS variant
	AND  (sub.parentid IS NULL OR sub.parentid <> a.toid);

COMMIT;
