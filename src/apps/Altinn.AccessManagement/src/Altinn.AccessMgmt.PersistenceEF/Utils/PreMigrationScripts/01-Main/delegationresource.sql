alter table dbo.delegationresource alter column audit_changedby drop not null;
alter table dbo.delegationresource alter column audit_changedbysystem drop not null;
alter table dbo.delegationresource alter column audit_changeoperation drop not null;
alter table dbo.delegationresource alter column audit_validfrom drop default;
alter table dbo.delegationresource drop constraint uc_delegationresource_delegationid_resourceid;

ALTER TABLE dbo.delegationresource RENAME CONSTRAINT fk_delegationresource_delegation_delegation TO fk_delegationresource_delegation_delegationid;
ALTER TABLE dbo.delegationresource DROP CONSTRAINT fk_delegationresource_resource_resource;

alter table dbo.delegationresource add constraint fk_delegationresource_resource_resourceid foreign key (resourceid) references dbo.resource on delete restrict;

ALTER INDEX dbo.uc_delegationresource_delegationid_resourceid_idx RENAME TO ix_delegationresource_delegationid_resourceid;
ALTER INDEX dbo.fk_delegationresource_delegationid_delegation_idx RENAME TO ix_delegationresource_delegationid;
ALTER INDEX dbo.fk_delegationresource_resourceid_resource_idx RENAME TO ix_delegationresource_resourceid;

ALTER TRIGGER delegationresource_audit_update ON dbo.delegationresource RENAME TO audit_delegationresource_update_trg;
ALTER TRIGGER delegationresource_audit_delete ON dbo.delegationresource RENAME TO audit_delegationresource_delete_trg;
drop trigger delegationresource_meta on dbo.delegationresource;

create or replace function dbo.audit_delegationresource_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditdelegationresource (
id,delegationid,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.delegationid,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_delegationresource_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditdelegationresource (
id,delegationid,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.delegationid,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

create or replace function dbo.audit_delegationresource_insert_fn() returns trigger
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

create or replace trigger audit_delegationresource_insert_trg
    before insert or update
    on dbo.delegationresource
    for each row
execute procedure dbo.audit_delegationresource_insert_fn();

