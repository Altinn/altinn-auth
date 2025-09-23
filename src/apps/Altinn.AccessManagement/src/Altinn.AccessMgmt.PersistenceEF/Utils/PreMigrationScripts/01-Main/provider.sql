alter table dbo.provider alter column audit_changedby drop not null;
alter table dbo.provider alter column audit_changedbysystem drop not null;
alter table dbo.provider alter column audit_changeoperation drop not null;
alter table dbo.provider alter column audit_validfrom drop default;

alter table dbo.provider alter column typeid set not null;
alter table dbo.provider drop constraint uc_provider_name;

ALTER TABLE dbo.provider DROP CONSTRAINT fk_provider_type_providertype;
alter table dbo.provider add constraint fk_provider_providertype_typeid foreign key (typeid) references dbo.providertype on delete restrict;

ALTER INDEX dbo.uc_provider_name_idx RENAME TO ix_provider_name;
ALTER INDEX dbo.fk_provider_typeid_providertype_idx RENAME TO ix_provider_typeid;

ALTER TRIGGER provider_audit_update ON dbo.provider RENAME TO audit_provider_update_trg;
ALTER TRIGGER provider_audit_delete ON dbo.provider RENAME TO audit_provider_delete_trg;
drop trigger provider_meta on dbo.provider;

create or replace function dbo.audit_provider_insert_fn() returns trigger
    language plpgsql
as
$$
DECLARE
    changed_by UUID;
    changed_by_system UUID;
    change_operation_id text;
BEGIN
    SELECT current_setting('app.changed_by', false)::uuid INTO changed_by;
    SELECT current_setting('app.changed_by_system', false)::uuid INTO changed_by_system;
    SELECT current_setting('app.change_operation_id', false) INTO change_operation_id;
    IF NEW.audit_changedby IS NULL THEN NEW.audit_changedby := changed_by; END IF;
    IF NEW.audit_changedbysystem IS NULL THEN NEW.audit_changedbysystem := changed_by_system; END IF;
    IF NEW.audit_changeoperation IS NULL THEN NEW.audit_changeoperation := change_operation_id; END IF;
    IF NEW.audit_validfrom IS NULL THEN NEW.audit_validfrom := now(); END IF;
    RETURN NEW;
END;
$$;

create or replace trigger audit_provider_insert_trg
    before insert or update
    on dbo.provider
    for each row
execute procedure dbo.audit_provider_insert_fn();

create or replace function dbo.audit_provider_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditprovider (
id,code,logourl,name,refid,typeid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.code,OLD.logourl,OLD.name,OLD.refid,OLD.typeid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_provider_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditprovider (
id,code,logourl,name,refid,typeid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.code,OLD.logourl,OLD.name,OLD.refid,OLD.typeid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

