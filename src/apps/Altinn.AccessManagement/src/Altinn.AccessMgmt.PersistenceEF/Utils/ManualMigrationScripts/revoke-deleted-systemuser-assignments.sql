-- ─────────────────────────────────────────────────────────────────────────────
-- Manual cleanup: revoke assignments given to system users that are already
-- marked as deleted.
--
-- Context (issue #3714)
--   When a system user is deleted in Authentication, Access Management receives
--   the party update from Register with isDeleted = true. The entity row is
--   updated, but until #3714 the assignments given to that system user were left
--   in place. PartySyncService now revokes them going forward (see
--   AssignmentService.ClearAssignmentsForDeletedSystemUser). This script removes
--   the rows left behind before that handling was deployed.
--
--   Mirrors the service logic: a system user only ever receives access, so every
--   assignment where the deleted system user is the to-party is removed regardless
--   of role. Deleting an Agent assignment cascades to dbo.delegation via the
--   existing ON DELETE CASCADE on delegation.toid, which in turn cascades to
--   delegationpackage / delegationresource. Assignment packages and resources
--   cascade the same way from dbo.assignment.
--
-- Reference identifiers
--   SystemUser entity type      : fe643898-2f47-4080-85e3-86bf6fe39630  (EntityTypeConstants.SystemUser)
--   RegisterImportSystem entity : efec83fc-deba-4f09-8073-b4dd19d0b16b  (SystemEntityConstants.RegisterImportSystem)
--
-- Execution
--   Run manually AFTER the #3714 change is deployed, so nothing new is left behind
--   between this run and the deploy.
--   Idempotent: only rows whose to-party is a deleted system user are touched,
--   so it can be re-run safely.
--
--   The audit triggers on dbo.assignment and dbo.delegation require an audit
--   context. We mimic the register import system as the actor, matching
--   AuditValues(SystemEntityConstants.RegisterImportSystem), by setting both the
--   SET LOCAL app.* settings and the session_audit_context temp table (read by
--   the delete triggers, including the ones fired by the cascade).
--
-- Before running: inspect what will be removed (same shape as the count in #3714).
--
--   SELECT r.name AS assignmenttype, count(*) AS numassignments
--   FROM   dbo.assignment a
--   JOIN   dbo.entity e ON e.id = a.toid
--   JOIN   dbo.role   r ON r.id = a.roleid
--   WHERE  e.typeid    = 'fe643898-2f47-4080-85e3-86bf6fe39630'
--     AND  e.isdeleted = true
--   GROUP BY r.name;
--
-- After running: the same query should return no rows.
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
		'revoke-deleted-systemuser-assignments'
);

SET LOCAL app.changed_by          = 'efec83fc-deba-4f09-8073-b4dd19d0b16b';
SET LOCAL app.changed_by_system   = 'efec83fc-deba-4f09-8073-b4dd19d0b16b';
SET LOCAL app.change_operation_id = 'revoke-deleted-systemuser-assignments';

-- Remove every assignment whose to-party is a system user marked as deleted.
-- Delegations, assignment packages and assignment resources tied to these
-- assignments are removed by the existing FK cascades.
DELETE FROM dbo.assignment AS a
USING  dbo.entity AS e
WHERE  e.id        = a.toid
	AND  e.typeid    = 'fe643898-2f47-4080-85e3-86bf6fe39630' -- SystemUser entity type
	AND  e.isdeleted = true;

COMMIT;
