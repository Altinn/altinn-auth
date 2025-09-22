alter table dbo.delegation alter column audit_changedby drop not null;
alter table dbo.delegation alter column audit_changedbysystem drop not null;
alter table dbo.delegation alter column audit_changeoperation drop not null;
alter table dbo.delegation alter column audit_validfrom drop default;
alter table dbo.delegation drop constraint uc_delegation_fromid_toid;

ALTER TABLE dbo.delegation RENAME CONSTRAINT fk_delegation_from_assignment TO fk_delegation_assignment_fromid;
ALTER TABLE dbo.delegation RENAME CONSTRAINT fk_delegation_to_assignment TO fk_delegation_assignment_toid;
ALTER TABLE dbo.delegation RENAME CONSTRAINT fk_delegation_facilitator_entity TO fk_delegation_entity_facilitatorid;

DROP INDEX dbo.uc_delegation_fromid_toid_idx;
ALTER INDEX dbo.fk_delegation_fromid_assignment_idx RENAME TO ix_delegation_fromid;
ALTER INDEX dbo.fk_delegation_toid_assignment_idx RENAME TO ix_delegation_toid;
ALTER INDEX dbo.fk_delegation_facilitatorid_entity_idx RENAME TO ix_delegation_facilitatorid;
create unique index ix_delegation_fromid_toid_facilitatorid on dbo.delegation (fromid, toid, facilitatorid);

ALTER TRIGGER delegation_audit_update ON dbo.delegation RENAME TO audit_delegation_update_trg;
ALTER TRIGGER delegation_audit_delete ON dbo.delegation RENAME TO audit_delegation_delete_trg;

drop trigger delegation_meta on dbo.delegation;

create or replace function dbo.audit_delegation_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;

create or REPLACE trigger audit_delegation_insert_trg
    before insert or update
    on dbo.delegation
    for each row
execute procedure dbo.audit_delegation_insert_fn();

create or replace function dbo.audit_delegation_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditdelegation (
id,facilitatorid,fromid,toid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.facilitatorid,OLD.fromid,OLD.toid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_delegation_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditdelegation (
id,facilitatorid,fromid,toid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.facilitatorid,OLD.fromid,OLD.toid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

