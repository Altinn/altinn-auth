alter table dbo.delegationpackage alter column audit_changedby drop not null;
alter table dbo.delegationpackage alter column audit_changedbysystem drop not null;
alter table dbo.delegationpackage alter column audit_changeoperation drop not null;
alter table dbo.delegationpackage alter column audit_validfrom drop default;
alter table dbo.delegationpackage drop constraint uc_delegationpackage_delegationid_packageid;

ALTER TABLE dbo.delegationpackage RENAME CONSTRAINT fk_delegationpackage_delegation_delegation TO fk_delegationpackage_delegation_delegationid;
ALTER TABLE dbo.delegationpackage drop CONSTRAINT fk_delegationpackage_package_package;
alter table dbo.delegationpackage drop constraint fk_delegationpackage_assignmentpackage_assignmentpackage;
alter table dbo.delegationpackage drop constraint fk_delegationpackage_rolepackage_rolepackage;

alter table dbo.delegationpackage add constraint fk_delegationpackage_package_packageid foreign key (packageid) references dbo.package on delete restrict;

ALTER INDEX dbo.fk_delegationpackage_delegationid_delegation_idx RENAME TO ix_delegationpackage_delegationid;
ALTER INDEX dbo.uc_delegationpackage_delegationid_packageid_idx RENAME TO ix_delegationpackage_delegationid_packageid;
ALTER INDEX dbo.fk_delegationpackage_packageid_package_idx RENAME TO ix_delegationpackage_packageid;

drop index dbo.fk_delegationpackage_assignmentpackageid_assignmentpackage_idx;
drop index dbo.fk_delegationpackage_rolepackageid_rolepackage_idx;

ALTER TRIGGER delegationpackage_audit_update ON dbo.delegationpackage RENAME TO audit_delegationpackage_update_trg;
ALTER TRIGGER delegationpackage_audit_delete ON dbo.delegationpackage RENAME TO audit_delegationpackage_delete_trg;
drop trigger delegationpackage_meta on dbo.delegationpackage;

create or replace function dbo.audit_delegationpackage_insert_fn() returns trigger
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

create or replace trigger audit_delegationpackage_insert_trg
    before insert or update
    on dbo.delegationpackage
    for each row
execute procedure dbo.audit_delegationpackage_insert_fn();

create or replace function dbo.audit_delegationpackage_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditdelegationpackage (
id,assignmentpackageid,delegationid,packageid,rolepackageid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.assignmentpackageid,OLD.delegationid,OLD.packageid,OLD.rolepackageid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_delegationpackage_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditdelegationpackage (
id,assignmentpackageid,delegationid,packageid,rolepackageid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.assignmentpackageid,OLD.delegationid,OLD.packageid,OLD.rolepackageid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;


