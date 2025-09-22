alter table dbo.rolepackage alter column audit_changedby drop not null;
alter table dbo.rolepackage alter column audit_changedbysystem drop not null;
alter table dbo.rolepackage alter column audit_changeoperation drop not null;
alter table dbo.rolepackage alter column audit_validfrom drop default;
alter table dbo.rolepackage alter column hasaccess set default false;
alter table dbo.rolepackage alter column candelegate set default false;
alter table dbo.rolepackage add canassign boolean not null default(false);

ALTER TABLE dbo.rolepackage drop CONSTRAINT fk_rolepackage_entityvariant_entityvariant;
ALTER TABLE dbo.rolepackage drop CONSTRAINT fk_rolepackage_package_package;
ALTER TABLE dbo.rolepackage RENAME CONSTRAINT fk_rolepackage_role_role TO fk_rolepackage_role_roleid;

alter table dbo.rolepackage
    add constraint fk_rolepackage_entityvariant_entityvariantid
        foreign key (entityvariantid) references dbo.entityvariant
            on delete restrict;

alter table dbo.rolepackage
    add constraint fk_rolepackage_package_packageid
        foreign key (packageid) references dbo.package
            on delete restrict;


ALTER INDEX dbo.fk_rolepackage_entityvariantid_entityvariant_idx RENAME TO ix_rolepackage_entityvariantid;
ALTER INDEX dbo.fk_rolepackage_packageid_package_idx RENAME TO ix_rolepackage_packageid;
ALTER INDEX dbo.fk_rolepackage_roleid_role_idx RENAME TO ix_rolepackage_roleid;

ALTER INDEX dbo.uc_rolepackage_roleid_packageid_m0 RENAME TO ix_rolepackage_roleid_packageid;
ALTER INDEX dbo.uc_rolepackage_roleid_packageid_m1 RENAME TO ix_rolepackage_roleid_packageid_entityvariantid;

ALTER TRIGGER rolepackage_audit_update ON dbo.rolepackage RENAME TO audit_rolepackage_update_trg;
ALTER TRIGGER rolepackage_audit_delete ON dbo.rolepackage RENAME TO audit_rolepackage_delete_trg;
drop trigger rolepackage_meta on dbo.rolepackage;

create or replace function dbo.audit_rolepackage_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;

create or replace trigger audit_rolepackage_insert_trg
    before insert or update
    on dbo.rolepackage
    for each row
execute procedure dbo.audit_rolepackage_insert_fn();

create or replace function dbo.audit_rolepackage_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditrolepackage (
id,canassign,candelegate,entityvariantid,hasaccess,packageid,roleid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.canassign,OLD.candelegate,OLD.entityvariantid,OLD.hasaccess,OLD.packageid,OLD.roleid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_rolepackage_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditrolepackage (
id,canassign,candelegate,entityvariantid,hasaccess,packageid,roleid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.canassign,OLD.candelegate,OLD.entityvariantid,OLD.hasaccess,OLD.packageid,OLD.roleid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

