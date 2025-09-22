alter table dbo.entitytype alter column audit_changedby drop not null;
alter table dbo.entitytype alter column audit_changedbysystem drop not null;
alter table dbo.entitytype alter column audit_changeoperation drop not null;
alter table dbo.entitytype alter column audit_validfrom drop default;

alter table dbo.entitytype drop constraint uc_entitytype_providerid_name;
ALTER TABLE dbo.entitytype drop CONSTRAINT fk_entitytype_provider_provider;

alter table dbo.entitytype add constraint fk_entitytype_provider_providerid foreign key (providerid) references dbo.provider on delete restrict;

drop index dbo.uc_entitytype_providerid_name_idx;
ALTER INDEX dbo.fk_entitytype_providerid_provider_idx RENAME TO ix_entitytype_providerid;
create unique index ix_entitytype_name_providerid on dbo.entitytype (name, providerid);


ALTER TRIGGER entitytype_audit_update ON dbo.entitytype RENAME TO audit_entitytype_update_trg;
ALTER TRIGGER entitytype_audit_delete ON dbo.entitytype RENAME TO audit_entitytype_delete_trg;

create or replace function dbo.audit_entitytype_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;


create or replace trigger audit_entitytype_insert_trg
    before insert or update
    on dbo.entitytype
    for each row
execute procedure dbo.audit_entitytype_insert_fn();

drop trigger entitytype_meta on dbo.entitytype;
create or replace function dbo.audit_entitytype_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditentitytype (
id,name,providerid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.name,OLD.providerid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_entitytype_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditentitytype (
id,name,providerid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.name,OLD.providerid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

