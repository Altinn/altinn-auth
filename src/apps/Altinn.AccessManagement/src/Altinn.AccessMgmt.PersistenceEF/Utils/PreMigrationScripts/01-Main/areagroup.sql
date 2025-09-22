alter table dbo.areagroup alter column audit_changedby drop not null;
alter table dbo.areagroup alter column audit_changedbysystem drop not null;
alter table dbo.areagroup alter column audit_changeoperation drop not null;
alter table dbo.areagroup alter column audit_validfrom drop default;
alter table dbo.areagroup alter column entitytypeid set not null;

alter table dbo.areagroup drop constraint uc_areagroup_name;
ALTER TABLE dbo.areagroup drop CONSTRAINT fk_areagroup_entitytype_entitytype;

alter table dbo.areagroup
    add constraint fk_areagroup_entitytype_entitytypeid
        foreign key (entitytypeid) references dbo.entitytype
            on delete restrict;

ALTER INDEX dbo.uc_areagroup_name_idx RENAME TO ix_areagroup_name;
ALTER INDEX dbo.fk_areagroup_entitytypeid_entitytype_idx RENAME TO ix_areagroup_entitytypeid;

drop trigger areagroup_meta on dbo.areagroup;
ALTER TRIGGER areagroup_audit_update ON dbo.areagroup RENAME TO audit_areagroup_update_trg;
ALTER TRIGGER areagroup_audit_delete ON dbo.areagroup RENAME TO audit_areagroup_delete_trg;

create or replace function dbo.audit_areagroup_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;

create or replace trigger audit_areagroup_insert_trg
    before insert or update
    on dbo.areagroup
    for each row
execute procedure dbo.audit_areagroup_insert_fn();

create or replace function dbo.audit_areagroup_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditareagroup (
id,description,entitytypeid,name,urn,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.description,OLD.entitytypeid,OLD.name,OLD.urn,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_areagroup_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditareagroup (
id,description,entitytypeid,name,urn,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.description,OLD.entitytypeid,OLD.name,OLD.urn,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

