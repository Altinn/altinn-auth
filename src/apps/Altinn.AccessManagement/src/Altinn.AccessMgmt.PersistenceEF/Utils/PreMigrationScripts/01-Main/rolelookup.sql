alter table dbo.rolelookup alter column audit_changedby drop not null;
alter table dbo.rolelookup alter column audit_changedbysystem drop not null;
alter table dbo.rolelookup alter column audit_changeoperation drop not null;
alter table dbo.rolelookup alter column audit_validfrom drop default;

alter table dbo.rolelookup drop constraint uc_rolelookup_roleid_key;
ALTER TABLE dbo.rolelookup RENAME CONSTRAINT fk_rolelookup_role_role TO fk_rolelookup_role_roleid;

ALTER INDEX dbo.fk_rolelookup_roleid_role_idx RENAME TO ix_rolelookup_roleid;
ALTER INDEX dbo.uc_rolelookup_roleid_key_idx RENAME TO ix_rolelookup_roleid_key;

ALTER TRIGGER rolelookup_audit_update ON dbo.rolelookup RENAME TO audit_rolelookup_update_trg;
ALTER TRIGGER rolelookup_audit_delete ON dbo.rolelookup RENAME TO audit_rolelookup_delete_trg;
drop trigger rolelookup_meta on dbo.rolelookup;

create or replace function dbo.audit_rolelookup_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;

create or replace trigger audit_rolelookup_insert_trg
    before insert or update
    on dbo.rolelookup
    for each row
execute procedure dbo.audit_rolelookup_insert_fn();

create or replace function dbo.audit_rolelookup_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditrolelookup (
id,key,roleid,value,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.key,OLD.roleid,OLD.value,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_rolelookup_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditrolelookup (
id,key,roleid,value,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.key,OLD.roleid,OLD.value,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

