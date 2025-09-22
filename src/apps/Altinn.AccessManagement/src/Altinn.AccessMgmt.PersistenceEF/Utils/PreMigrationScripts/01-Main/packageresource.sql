alter table dbo.packageresource alter column audit_changedby drop not null;
alter table dbo.packageresource alter column audit_changedbysystem drop not null;
alter table dbo.packageresource alter column audit_changeoperation drop not null;
alter table dbo.packageresource alter column audit_validfrom drop default;

alter table dbo.packageresource drop constraint uc_packageresource_packageid_resourceid;
ALTER TABLE dbo.packageresource RENAME CONSTRAINT fk_packageresource_package_package TO fk_packageresource_package_packageid;
ALTER TABLE dbo.packageresource drop CONSTRAINT fk_packageresource_resource_resource;

alter table dbo.packageresource add constraint fk_packageresource_resource_resourceid foreign key (resourceid) references dbo.resource on delete restrict;

ALTER INDEX dbo.fk_packageresource_packageid_package_idx RENAME TO ix_packageresource_packageid;
ALTER INDEX dbo.fk_packageresource_resourceid_resource_idx RENAME TO ix_packageresource_resourceid;
ALTER INDEX dbo.uc_packageresource_packageid_resourceid_idx RENAME TO ix_packageresource_packageid_resourceid;

ALTER TRIGGER packageresource_audit_update ON dbo.packageresource RENAME TO audit_packageresource_update_trg;
ALTER TRIGGER packageresource_audit_delete ON dbo.packageresource RENAME TO audit_packageresource_delete_trg;
drop trigger packageresource_meta on dbo.packageresource;

create or replace function dbo.audit_packageresource_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;

create or replace trigger audit_packageresource_insert_trg
    before insert or update
    on dbo.packageresource
    for each row
execute procedure dbo.audit_packageresource_insert_fn();

create or replace function dbo.audit_packageresource_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditpackageresource (
id,packageid,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.packageid,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_packageresource_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditpackageresource (
id,packageid,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.packageid,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

