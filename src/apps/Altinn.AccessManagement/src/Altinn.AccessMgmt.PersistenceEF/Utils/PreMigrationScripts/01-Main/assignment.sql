alter table dbo.assignment alter column audit_changedby drop not null;
alter table dbo.assignment alter column audit_changedbysystem drop not null;
alter table dbo.assignment alter column audit_changeoperation drop not null;
alter table dbo.assignment alter column audit_validfrom drop default;

ALTER TABLE dbo.assignment RENAME CONSTRAINT fk_assignment_from_entity TO fk_assignment_entity_fromid;
ALTER TABLE dbo.assignment RENAME CONSTRAINT fk_assignment_to_entity TO fk_assignment_entity_toid;
ALTER TABLE dbo.assignment DROP CONSTRAINT fk_assignment_role_role;
alter table dbo.assignment drop constraint uc_assignment_fromid_toid_roleid;

alter table dbo.assignment add constraint fk_assignment_role_roleid foreign key (roleid) references dbo.role on delete restrict;

ALTER INDEX dbo.fk_assignment_fromid_entity_idx RENAME TO ix_assignment_fromid;
ALTER INDEX dbo.fk_assignment_toid_entity_idx RENAME TO ix_assignment_toid;
ALTER INDEX dbo.fk_assignment_roleid_role_idx RENAME TO ix_assignment_roleid;
ALTER INDEX dbo.uc_assignment_fromid_toid_roleid_idx RENAME TO ix_assignment_fromid_toid_roleid;

ALTER TRIGGER assignment_audit_update ON dbo.assignment RENAME TO audit_assignment_update_trg;
ALTER TRIGGER assignment_audit_delete ON dbo.assignment RENAME TO audit_assignment_delete_trg;
drop trigger assignment_meta on dbo.assignment;

create or replace  function dbo.audit_assignment_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;

create or replace function dbo.audit_assignment_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditassignment (
id,fromid,roleid,toid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.fromid,OLD.roleid,OLD.toid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_assignment_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditassignment (
id,fromid,roleid,toid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.fromid,OLD.roleid,OLD.toid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

create or replace trigger audit_assignment_insert_trg
    before insert or update
    on dbo.assignment
    for each row
execute procedure dbo.audit_assignment_insert_fn();


