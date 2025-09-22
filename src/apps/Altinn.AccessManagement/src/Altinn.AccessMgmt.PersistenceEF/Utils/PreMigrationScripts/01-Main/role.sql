alter table dbo.role alter column audit_changedby drop not null;
alter table dbo.role alter column audit_changedbysystem drop not null;
alter table dbo.role alter column audit_changeoperation drop not null;
alter table dbo.role alter column audit_validfrom drop default;

alter table dbo.role drop constraint uc_role_urn;
alter table dbo.role drop constraint uc_role_providerid_name;
alter table dbo.role drop constraint uc_role_providerid_code;
ALTER TABLE dbo.role drop CONSTRAINT fk_role_provider_provider;
ALTER TABLE dbo.role drop CONSTRAINT fk_role_entitytype_entitytype;

alter table dbo.role
    add constraint fk_role_entitytype_entitytypeid
        foreign key (entitytypeid) references dbo.entitytype
            on delete restrict;

alter table dbo.role
    add constraint fk_role_provider_providerid
        foreign key (providerid) references dbo.provider
            on delete restrict;

ALTER INDEX dbo.uc_role_urn_idx RENAME TO ix_role_urn;
ALTER INDEX dbo.uc_role_providerid_name_idx RENAME TO ix_role_providerid_name;
ALTER INDEX dbo.uc_role_providerid_code_idx RENAME TO ix_role_providerid_code;
ALTER INDEX dbo.fk_role_entitytypeid_entitytype_idx RENAME TO ix_role_entitytypeid;
ALTER INDEX dbo.fk_role_providerid_provider_idx RENAME TO ix_role_providerid;

ALTER TRIGGER role_audit_update ON dbo.role RENAME TO audit_role_update_trg;
ALTER TRIGGER role_audit_delete ON dbo.role RENAME TO audit_role_delete_trg;
drop trigger role_meta on dbo.role;

create or replace function dbo.audit_role_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditrole (
id,code,description,entitytypeid,isassignable,iskeyrole,name,providerid,urn,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.code,OLD.description,OLD.entitytypeid,OLD.isassignable,OLD.iskeyrole,OLD.name,OLD.providerid,OLD.urn,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_role_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditrole (
id,code,description,entitytypeid,isassignable,iskeyrole,name,providerid,urn,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.code,OLD.description,OLD.entitytypeid,OLD.isassignable,OLD.iskeyrole,OLD.name,OLD.providerid,OLD.urn,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

create or replace function dbo.audit_role_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;

create or replace trigger audit_role_insert_trg
    before insert or update
    on dbo.role
    for each row
execute procedure dbo.audit_role_insert_fn();

