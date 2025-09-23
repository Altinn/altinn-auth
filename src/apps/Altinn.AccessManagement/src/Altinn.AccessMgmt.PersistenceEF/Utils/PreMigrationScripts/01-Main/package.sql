alter table dbo.package alter column audit_changedby drop not null;
alter table dbo.package alter column audit_changedbysystem drop not null;
alter table dbo.package alter column audit_changeoperation drop not null;
alter table dbo.package alter column audit_validfrom drop default;

alter table dbo.package alter column name drop not null;
alter table dbo.package alter column description drop not null;
alter table dbo.package alter column urn drop not null;

alter table dbo.package drop constraint uc_package_providerid_name;

ALTER TABLE dbo.package DROP CONSTRAINT fk_package_provider_provider;
ALTER TABLE dbo.package DROP CONSTRAINT fk_package_entitytype_entitytype;
ALTER TABLE dbo.package DROP CONSTRAINT fk_package_area_area;

alter table dbo.package add constraint fk_package_area_areaid foreign key (areaid) references dbo.area on delete restrict;
alter table dbo.package add constraint fk_package_entitytype_entitytypeid foreign key (entitytypeid) references dbo.entitytype on delete restrict;
alter table dbo.package add constraint fk_package_provider_providerid foreign key (providerid) references dbo.provider on delete restrict;

ALTER INDEX dbo.uc_package_providerid_name_idx RENAME TO ix_package_providerid_name;

ALTER INDEX dbo.fk_package_areaid_area_idx RENAME TO ix_package_areaid;
ALTER INDEX dbo.fk_package_entitytypeid_entitytype_idx RENAME TO ix_package_entitytypeid;
ALTER INDEX dbo.fk_package_providerid_provider_idx RENAME TO ix_package_providerid;

ALTER TRIGGER package_audit_update ON dbo.package RENAME TO audit_package_update_trg;
ALTER TRIGGER package_audit_delete ON dbo.package RENAME TO audit_package_delete_trg;
drop trigger package_meta on dbo.package;

create or replace function dbo.audit_package_insert_fn() returns trigger
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

create or replace trigger audit_package_insert_trg
    before insert or update
    on dbo.package
    for each row
execute procedure dbo.audit_package_insert_fn();

create or replace function dbo.audit_package_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditpackage (
id,areaid,description,entitytypeid,hasresources,isassignable,isdelegable,name,providerid,urn,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.areaid,OLD.description,OLD.entitytypeid,OLD.hasresources,OLD.isassignable,OLD.isdelegable,OLD.name,OLD.providerid,OLD.urn,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_package_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditpackage (
id,areaid,description,entitytypeid,hasresources,isassignable,isdelegable,name,providerid,urn,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.areaid,OLD.description,OLD.entitytypeid,OLD.hasresources,OLD.isassignable,OLD.isdelegable,OLD.name,OLD.providerid,OLD.urn,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

