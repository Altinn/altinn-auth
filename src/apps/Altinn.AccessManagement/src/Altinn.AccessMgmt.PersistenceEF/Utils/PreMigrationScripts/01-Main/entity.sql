alter table dbo.entity alter column audit_changedby drop not null;
alter table dbo.entity alter column audit_changedbysystem drop not null;
alter table dbo.entity alter column audit_changeoperation drop not null;
alter table dbo.entity alter column audit_validfrom drop default;
alter table dbo.entity alter column refid drop not null;

ALTER INDEX dbo.fk_entity_parentid_entity_idx RENAME TO ix_entity_parentid;
ALTER INDEX dbo.fk_entity_typeid_entitytype_idx RENAME TO ix_entity_typeid;
ALTER INDEX dbo.fk_entity_variantid_entityvariant_idx RENAME TO ix_entity_variantid;

alter table dbo.entity drop constraint fk_entity_type_entitytype;
alter table dbo.entity drop constraint fk_entity_variant_entityvariant;
alter table dbo.entity drop constraint fk_entity_parent_entity;

alter table dbo.entity
    add constraint fk_entity_entity_parentid
        foreign key (parentid) references dbo.entity
            on delete restrict;

alter table dbo.entity
    add constraint fk_entity_entitytype_typeid
        foreign key (typeid) references dbo.entitytype
            on delete restrict;

alter table dbo.entity
    add constraint fk_entity_entityvariant_variantid
        foreign key (variantid) references dbo.entityvariant
            on delete restrict;

ALTER TRIGGER entity_audit_update ON dbo.entity RENAME TO audit_entity_update_trg;
ALTER TRIGGER entity_audit_delete ON dbo.entity RENAME TO audit_entity_delete_trg;
drop trigger entity_meta on dbo.entity;

create or replace function dbo.audit_entity_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditentity (
id,name,parentid,refid,typeid,variantid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.name,OLD.parentid,OLD.refid,OLD.typeid,OLD.variantid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_entity_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditentity (
id,name,parentid,refid,typeid,variantid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.name,OLD.parentid,OLD.refid,OLD.typeid,OLD.variantid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

create or replace function dbo.audit_entity_insert_fn() returns trigger
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

create or replace trigger audit_entity_insert_trg
    before insert or update
    on dbo.entity
    for each row
execute procedure dbo.audit_entity_insert_fn();

