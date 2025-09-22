alter table dbo.roleresource alter column audit_changedby drop not null;
alter table dbo.roleresource alter column audit_changedbysystem drop not null;
alter table dbo.roleresource alter column audit_changeoperation drop not null;
alter table dbo.roleresource alter column audit_validfrom drop default;

alter table dbo.roleresource drop constraint uc_roleresource_roleid_resourceid;
ALTER TABLE dbo.roleresource drop CONSTRAINT fk_roleresource_resource_resource;
ALTER TABLE dbo.roleresource RENAME CONSTRAINT fk_roleresource_role_role TO fk_roleresource_role_roleid;

alter table dbo.roleresource
    add constraint fk_roleresource_resource_resourceid
        foreign key (resourceid) references dbo.resource
            on delete restrict;


ALTER INDEX dbo.fk_roleresource_resourceid_resource_idx RENAME TO ix_roleresource_resourceid;
ALTER INDEX dbo.fk_roleresource_roleid_role_idx RENAME TO ix_roleresource_roleid;
ALTER INDEX dbo.uc_roleresource_roleid_resourceid_idx RENAME TO ix_roleresource_roleid_resourceid;

ALTER TRIGGER roleresource_audit_update ON dbo.roleresource RENAME TO audit_roleresource_update_trg;
ALTER TRIGGER roleresource_audit_delete ON dbo.roleresource RENAME TO audit_roleresource_delete_trg;
drop trigger roleresource_meta on dbo.roleresource;

create or replace function dbo.audit_roleresource_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;

create or replace trigger audit_roleresource_insert_trg
    before insert or update
    on dbo.roleresource
    for each row
execute procedure dbo.audit_roleresource_insert_fn();

create or replace function dbo.audit_roleresource_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditroleresource (
id,resourceid,roleid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.resourceid,OLD.roleid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_roleresource_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditroleresource (
id,resourceid,roleid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.resourceid,OLD.roleid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

