alter table dbo.entityvariantrole alter column audit_changedby drop not null;
alter table dbo.entityvariantrole alter column audit_changedbysystem drop not null;
alter table dbo.entityvariantrole alter column audit_changeoperation drop not null;
alter table dbo.entityvariantrole alter column audit_validfrom drop default;

alter table dbo.entityvariantrole drop constraint uc_entityvariantrole_variantid_roleid;
ALTER TABLE dbo.entityvariantrole drop CONSTRAINT fk_entityvariantrole_role_role;
ALTER TABLE dbo.entityvariantrole drop CONSTRAINT fk_entityvariantrole_variant_entityvariant;

alter table dbo.entityvariantrole add constraint fk_entityvariantrole_entityvariant_variantid foreign key (variantid) references dbo.entityvariant on delete cascade;
alter table dbo.entityvariantrole add constraint fk_entityvariantrole_role_roleid foreign key (roleid) references dbo.role on delete restrict;

ALTER INDEX dbo.fk_entityvariantrole_variantid_entityvariant_idx RENAME TO ix_entityvariantrole_variantid;
ALTER INDEX dbo.fk_entityvariantrole_roleid_role_idx RENAME TO ix_entityvariantrole_roleid;
ALTER INDEX dbo.uc_entityvariantrole_variantid_roleid_idx RENAME TO ix_entityvariantrole_variantid_roleid;

ALTER TRIGGER entityvariantrole_audit_update ON dbo.entityvariantrole RENAME TO audit_entityvariantrole_update_trg;
ALTER TRIGGER entityvariantrole_audit_delete ON dbo.entityvariantrole RENAME TO audit_entityvariantrole_delete_trg;
drop trigger entityvariantrole_meta on dbo.entityvariantrole;

create or replace function dbo.audit_entityvariantrole_insert_fn() returns trigger
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

create or replace trigger audit_entityvariantrole_insert_trg
    before insert or update
    on dbo.entityvariantrole
    for each row
execute procedure dbo.audit_entityvariantrole_insert_fn();

create or replace function dbo.audit_entityvariantrole_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditentityvariantrole (
id,roleid,variantid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.roleid,OLD.variantid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_entityvariantrole_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditentityvariantrole (
id,roleid,variantid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.roleid,OLD.variantid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

