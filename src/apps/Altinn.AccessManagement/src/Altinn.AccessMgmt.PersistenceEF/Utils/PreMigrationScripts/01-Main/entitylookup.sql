alter table dbo.entitylookup alter column audit_changedby drop not null;
alter table dbo.entitylookup alter column audit_changedbysystem drop not null;
alter table dbo.entitylookup alter column audit_changeoperation drop not null;
alter table dbo.entitylookup alter column audit_validfrom drop default;

alter table dbo.entitylookup drop constraint uc_entitylookup_entityid_key;
ALTER TABLE dbo.entitylookup RENAME CONSTRAINT fk_entitylookup_entity_entity TO fk_entitylookup_entity_entityid;

ALTER INDEX dbo.fk_entitylookup_entityid_entity_idx RENAME TO ix_entitylookup_entityid;
ALTER INDEX dbo.uc_entitylookup_entityid_key_idx RENAME TO ix_entitylookup_entityid_key;
drop index dbo.entitylookup_key_value_idx;

ALTER TRIGGER entitylookup_audit_update ON dbo.entitylookup RENAME TO audit_entitylookup_update_trg;
ALTER TRIGGER entitylookup_audit_delete ON dbo.entitylookup RENAME TO audit_entitylookup_delete_trg;
drop trigger entitylookup_meta on dbo.entitylookup;

create or replace function dbo.audit_entitytype_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;

create or replace function dbo.audit_entitylookup_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;

create or replace trigger audit_entitylookup_insert_trg
    before insert or update
    on dbo.entitylookup
    for each row
execute procedure dbo.audit_entitylookup_insert_fn();

create or replace function dbo.audit_entitylookup_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditentitylookup (
id,entityid,isprotected,key,value,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.entityid,OLD.isprotected,OLD.key,OLD.value,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_entitylookup_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditentitylookup (
id,entityid,isprotected,key,value,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.entityid,OLD.isprotected,OLD.key,OLD.value,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

