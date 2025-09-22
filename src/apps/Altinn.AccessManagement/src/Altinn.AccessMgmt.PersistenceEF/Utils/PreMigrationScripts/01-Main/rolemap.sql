alter table dbo.rolemap alter column audit_changedby drop not null;
alter table dbo.rolemap alter column audit_changedbysystem drop not null;
alter table dbo.rolemap alter column audit_changeoperation drop not null;
alter table dbo.rolemap alter column audit_validfrom drop default;

alter table dbo.rolemap drop constraint uc_rolemap_hasroleid_getroleid;
ALTER TABLE dbo.rolemap RENAME CONSTRAINT fk_rolemap_getrole_role TO fk_rolemap_role_getroleid;
ALTER TABLE dbo.rolemap RENAME CONSTRAINT fk_rolemap_hasrole_role TO fk_rolemap_role_hasroleid;

ALTER INDEX dbo.uc_rolemap_hasroleid_getroleid_idx RENAME TO ix_rolemap_hasroleid_getroleid;
ALTER INDEX dbo.fk_rolemap_getroleid_role_idx RENAME TO ix_rolemap_getroleid;
ALTER INDEX dbo.fk_rolemap_hasroleid_role_idx RENAME TO ix_rolemap_hasroleid;

ALTER TRIGGER rolemap_audit_update ON dbo.rolemap RENAME TO audit_rolemap_update_trg;
ALTER TRIGGER rolemap_audit_delete ON dbo.rolemap RENAME TO audit_rolemap_delete_trg;
drop trigger rolemap_meta on dbo.rolemap;

create or replace function dbo.audit_rolemap_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;

create or replace trigger audit_rolemap_insert_trg
    before insert or update
    on dbo.rolemap
    for each row
execute procedure dbo.audit_rolemap_insert_fn();

create or replace function dbo.audit_rolemap_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditrolemap (
id,getroleid,hasroleid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.getroleid,OLD.hasroleid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_rolemap_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditrolemap (
id,getroleid,hasroleid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.getroleid,OLD.hasroleid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

