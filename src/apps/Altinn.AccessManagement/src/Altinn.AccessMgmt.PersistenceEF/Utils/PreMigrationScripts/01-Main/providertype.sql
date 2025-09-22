alter table dbo.providertype alter column audit_changedby drop not null;
alter table dbo.providertype alter column audit_changedbysystem drop not null;
alter table dbo.providertype alter column audit_changeoperation drop not null;
alter table dbo.providertype alter column audit_validfrom drop default;

alter table dbo.providertype drop constraint uc_providertype_name;

ALTER INDEX dbo.uc_providertype_name_idx RENAME TO ix_providertype_name;

ALTER TRIGGER providertype_audit_update ON dbo.providertype RENAME TO audit_providertype_update_trg;
ALTER TRIGGER providertype_audit_delete ON dbo.providertype RENAME TO audit_providertype_delete_trg;
drop trigger providertype_meta on dbo.providertype;

create or replace function dbo.audit_providertype_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditprovidertype (
id,name,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.name,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_providertype_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditprovidertype (
id,name,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.name,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

create or replace function dbo.audit_providertype_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;

create or replace trigger audit_providertype_insert_trg
    before insert or update
    on dbo.providertype
    for each row
execute procedure dbo.audit_providertype_insert_fn();

