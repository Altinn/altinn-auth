alter table dbo.area drop constraint uc_area_name;

alter table dbo.area alter column audit_changedby drop not null;
alter table dbo.area alter column audit_changedbysystem drop not null;
alter table dbo.area alter column audit_changeoperation drop not null;
alter table dbo.area alter column audit_validfrom drop default;

ALTER TABLE dbo.area RENAME CONSTRAINT fk_area_group_areagroup TO fk_area_areagroup_groupid;

ALTER INDEX dbo.uc_area_name_idx RENAME TO ix_area_name;
ALTER INDEX dbo.fk_area_groupid_areagroup_idx RENAME TO ix_area_groupid;

ALTER TRIGGER area_audit_update ON dbo.area RENAME TO audit_area_update_trg;
ALTER TRIGGER area_audit_delete ON dbo.area RENAME TO audit_area_delete_trg;

drop trigger area_meta on dbo.area;

create or replace function dbo.audit_area_insert_fn() returns trigger
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

create or replace trigger audit_area_insert_trg
    before insert or update
    on dbo.area
    for each row
execute procedure dbo.audit_area_insert_fn();

create or replace function dbo.audit_area_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditarea (
id,description,groupid,iconurl,name,urn,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.description,OLD.groupid,OLD.iconurl,OLD.name,OLD.urn,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_area_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditarea (
id,description,groupid,iconurl,name,urn,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.description,OLD.groupid,OLD.iconurl,OLD.name,OLD.urn,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

