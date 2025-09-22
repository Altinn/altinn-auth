alter table dbo.entityvariant alter column audit_changedby drop not null;
alter table dbo.entityvariant alter column audit_changedbysystem drop not null;
alter table dbo.entityvariant alter column audit_changeoperation drop not null;
alter table dbo.entityvariant alter column audit_validfrom drop default;

alter table dbo.entityvariant drop constraint uc_entityvariant_typeid_name;
ALTER TABLE dbo.entityvariant RENAME CONSTRAINT fk_entityvariant_type_entitytype TO fk_entityvariant_entitytype_typeid;

DROP INDEX dbo.uc_entityvariant_typeid_name_idx;
ALTER INDEX dbo.fk_entityvariant_typeid_entitytype_idx RENAME TO ix_entityvariant_typeid;
create unique index ix_entityvariant_name_typeid on dbo.entityvariant (name, typeid);

ALTER TRIGGER entityvariant_audit_update ON dbo.entityvariant RENAME TO audit_entityvariant_update_trg;
ALTER TRIGGER entityvariant_audit_delete ON dbo.entityvariant RENAME TO audit_entityvariant_delete_trg;
drop trigger entityvariant_meta on dbo.entityvariant;

create or replace function dbo.audit_entityvariant_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;

create or replace trigger audit_entityvariant_insert_trg
    before insert or update
    on dbo.entityvariant
    for each row
execute procedure dbo.audit_entityvariant_insert_fn();

create or replace function dbo.audit_entityvariant_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditentityvariant (
id,description,name,typeid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.description,OLD.name,OLD.typeid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_entityvariant_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditentityvariant (
id,description,name,typeid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.description,OLD.name,OLD.typeid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

