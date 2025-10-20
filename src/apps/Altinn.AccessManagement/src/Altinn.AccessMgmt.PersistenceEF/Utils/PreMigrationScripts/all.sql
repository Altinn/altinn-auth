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

alter table dbo.assignment alter column audit_changedby drop not null;
alter table dbo.assignment alter column audit_changedbysystem drop not null;
alter table dbo.assignment alter column audit_changeoperation drop not null;
alter table dbo.assignment alter column audit_validfrom drop default;

ALTER TABLE dbo.assignment RENAME CONSTRAINT fk_assignment_from_entity TO fk_assignment_entity_fromid;
ALTER TABLE dbo.assignment RENAME CONSTRAINT fk_assignment_to_entity TO fk_assignment_entity_toid;
ALTER TABLE dbo.assignment DROP CONSTRAINT fk_assignment_role_role;
alter table dbo.assignment drop constraint uc_assignment_fromid_toid_roleid;

alter table dbo.assignment add constraint fk_assignment_role_roleid foreign key (roleid) references dbo.role on delete restrict;

ALTER INDEX dbo.fk_assignment_fromid_entity_idx RENAME TO ix_assignment_fromid;
ALTER INDEX dbo.fk_assignment_toid_entity_idx RENAME TO ix_assignment_toid;
ALTER INDEX dbo.fk_assignment_roleid_role_idx RENAME TO ix_assignment_roleid;
ALTER INDEX dbo.uc_assignment_fromid_toid_roleid_idx RENAME TO ix_assignment_fromid_toid_roleid;

ALTER TRIGGER assignment_audit_update ON dbo.assignment RENAME TO audit_assignment_update_trg;
ALTER TRIGGER assignment_audit_delete ON dbo.assignment RENAME TO audit_assignment_delete_trg;
drop trigger assignment_meta on dbo.assignment;

create or replace  function dbo.audit_assignment_insert_fn() returns trigger
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

create or replace function dbo.audit_assignment_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditassignment (
id,fromid,roleid,toid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.fromid,OLD.roleid,OLD.toid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_assignment_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditassignment (
id,fromid,roleid,toid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.fromid,OLD.roleid,OLD.toid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

create or replace trigger audit_assignment_insert_trg
    before insert or update
    on dbo.assignment
    for each row
execute procedure dbo.audit_assignment_insert_fn();

alter table dbo.assignmentpackage alter column audit_changedby drop not null;
alter table dbo.assignmentpackage alter column audit_changedbysystem drop not null;
alter table dbo.assignmentpackage alter column audit_changeoperation drop not null;
alter table dbo.assignmentpackage alter column audit_validfrom drop default;

alter table dbo.assignmentpackage drop constraint uc_assignmentpackage_assignmentid_packageid;
ALTER TABLE dbo.assignmentpackage drop CONSTRAINT fk_assignmentpackage_package_package;
ALTER TABLE dbo.assignmentpackage RENAME CONSTRAINT fk_assignmentpackage_assignment_assignment TO fk_assignmentpackage_assignment_assignmentid;

alter table dbo.assignmentpackage add constraint fk_assignmentpackage_package_packageid foreign key (packageid) references dbo.package on delete restrict;

ALTER INDEX dbo.uc_assignmentpackage_assignmentid_packageid_idx RENAME TO ix_assignmentpackage_assignmentid_packageid;
ALTER INDEX dbo.fk_assignmentpackage_assignmentid_assignment_idx RENAME TO ix_assignmentpackage_assignmentid;
ALTER INDEX dbo.fk_assignmentpackage_packageid_package_idx RENAME TO ix_assignmentpackage_packageid;

ALTER TRIGGER assignmentpackage_audit_update ON dbo.assignmentpackage RENAME TO audit_assignmentpackage_update_trg;
ALTER TRIGGER assignmentpackage_audit_delete ON dbo.assignmentpackage RENAME TO audit_assignmentpackage_delete_trg;

drop trigger assignmentpackage_meta on dbo.assignmentpackage;

create or replace function dbo.audit_assignmentpackage_insert_fn() returns trigger
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

create or replace trigger audit_assignmentpackage_insert_trg
    before insert or update
    on dbo.assignmentpackage
    for each row
execute procedure dbo.audit_assignmentpackage_insert_fn();

create or replace function dbo.audit_assignmentpackage_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditassignmentpackage (
id,assignmentid,packageid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.assignmentid,OLD.packageid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_assignmentpackage_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditassignmentpackage (
id,assignmentid,packageid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.assignmentid,OLD.packageid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

alter table dbo.assignmentresource alter column audit_changedby drop not null;
alter table dbo.assignmentresource alter column audit_changedbysystem drop not null;
alter table dbo.assignmentresource alter column audit_changeoperation drop not null;
alter table dbo.assignmentresource alter column audit_validfrom drop default;
alter table dbo.assignmentresource drop constraint uc_assignmentresource_assignmentid_resourceid;

ALTER TABLE dbo.assignmentresource RENAME CONSTRAINT fk_assignmentresource_assignment_assignment TO fk_assignmentresource_assignment_assignmentid;
ALTER TABLE dbo.assignmentresource DROP CONSTRAINT fk_assignmentresource_resource_resource;

alter table dbo.assignmentresource add constraint fk_assignmentresource_resource_resourceid foreign key (resourceid) references dbo.resource on delete restrict;

ALTER INDEX dbo.uc_assignmentresource_assignmentid_resourceid_idx RENAME TO ix_assignmentresource_assignmentid_resourceid;
ALTER INDEX dbo.fk_assignmentresource_assignmentid_assignment_idx RENAME TO ix_assignmentresource_assignmentid;
ALTER INDEX dbo.fk_assignmentresource_resourceid_resource_idx RENAME TO ix_assignmentresource_resourceid;

ALTER TRIGGER assignmentresource_audit_update ON dbo.assignmentresource RENAME TO audit_assignmentresource_update_trg;
ALTER TRIGGER assignmentresource_audit_delete ON dbo.assignmentresource RENAME TO audit_assignmentresource_delete_trg;
drop trigger assignmentresource_meta on dbo.assignmentresource;

create or replace function dbo.audit_assignmentresource_insert_fn() returns trigger
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

create or replace trigger audit_assignmentresource_insert_trg
    before insert or update
    on dbo.assignmentresource
    for each row
execute procedure dbo.audit_assignmentresource_insert_fn();

create or replace function dbo.audit_assignmentresource_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditassignmentresource (
id,assignmentid,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.assignmentid,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_assignmentresource_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditassignmentresource (
id,assignmentid,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.assignmentid,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

alter table dbo.delegation alter column audit_changedby drop not null;
alter table dbo.delegation alter column audit_changedbysystem drop not null;
alter table dbo.delegation alter column audit_changeoperation drop not null;
alter table dbo.delegation alter column audit_validfrom drop default;
alter table dbo.delegation drop constraint uc_delegation_fromid_toid;

ALTER TABLE dbo.delegation RENAME CONSTRAINT fk_delegation_from_assignment TO fk_delegation_assignment_fromid;
ALTER TABLE dbo.delegation RENAME CONSTRAINT fk_delegation_to_assignment TO fk_delegation_assignment_toid;
ALTER TABLE dbo.delegation RENAME CONSTRAINT fk_delegation_facilitator_entity TO fk_delegation_entity_facilitatorid;

DROP INDEX dbo.uc_delegation_fromid_toid_idx;
ALTER INDEX dbo.fk_delegation_fromid_assignment_idx RENAME TO ix_delegation_fromid;
ALTER INDEX dbo.fk_delegation_toid_assignment_idx RENAME TO ix_delegation_toid;
ALTER INDEX dbo.fk_delegation_facilitatorid_entity_idx RENAME TO ix_delegation_facilitatorid;
create unique index ix_delegation_fromid_toid_facilitatorid on dbo.delegation (fromid, toid, facilitatorid);

ALTER TRIGGER delegation_audit_update ON dbo.delegation RENAME TO audit_delegation_update_trg;
ALTER TRIGGER delegation_audit_delete ON dbo.delegation RENAME TO audit_delegation_delete_trg;

drop trigger delegation_meta on dbo.delegation;

create or replace function dbo.audit_delegation_insert_fn() returns trigger
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

create or REPLACE trigger audit_delegation_insert_trg
    before insert or update
    on dbo.delegation
    for each row
execute procedure dbo.audit_delegation_insert_fn();

create or replace function dbo.audit_delegation_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditdelegation (
id,facilitatorid,fromid,toid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.facilitatorid,OLD.fromid,OLD.toid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_delegation_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditdelegation (
id,facilitatorid,fromid,toid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.facilitatorid,OLD.fromid,OLD.toid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

alter table dbo.delegationpackage alter column audit_changedby drop not null;
alter table dbo.delegationpackage alter column audit_changedbysystem drop not null;
alter table dbo.delegationpackage alter column audit_changeoperation drop not null;
alter table dbo.delegationpackage alter column audit_validfrom drop default;
alter table dbo.delegationpackage drop constraint uc_delegationpackage_delegationid_packageid;

ALTER TABLE dbo.delegationpackage RENAME CONSTRAINT fk_delegationpackage_delegation_delegation TO fk_delegationpackage_delegation_delegationid;
ALTER TABLE dbo.delegationpackage drop CONSTRAINT fk_delegationpackage_package_package;
alter table dbo.delegationpackage drop constraint fk_delegationpackage_assignmentpackage_assignmentpackage;
alter table dbo.delegationpackage drop constraint fk_delegationpackage_rolepackage_rolepackage;

alter table dbo.delegationpackage add constraint fk_delegationpackage_package_packageid foreign key (packageid) references dbo.package on delete restrict;

ALTER INDEX dbo.fk_delegationpackage_delegationid_delegation_idx RENAME TO ix_delegationpackage_delegationid;
ALTER INDEX dbo.uc_delegationpackage_delegationid_packageid_idx RENAME TO ix_delegationpackage_delegationid_packageid;
ALTER INDEX dbo.fk_delegationpackage_packageid_package_idx RENAME TO ix_delegationpackage_packageid;

drop index dbo.fk_delegationpackage_assignmentpackageid_assignmentpackage_idx;
drop index dbo.fk_delegationpackage_rolepackageid_rolepackage_idx;

ALTER TRIGGER delegationpackage_audit_update ON dbo.delegationpackage RENAME TO audit_delegationpackage_update_trg;
ALTER TRIGGER delegationpackage_audit_delete ON dbo.delegationpackage RENAME TO audit_delegationpackage_delete_trg;
drop trigger delegationpackage_meta on dbo.delegationpackage;

create or replace function dbo.audit_delegationpackage_insert_fn() returns trigger
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

create or replace trigger audit_delegationpackage_insert_trg
    before insert or update
    on dbo.delegationpackage
    for each row
execute procedure dbo.audit_delegationpackage_insert_fn();

create or replace function dbo.audit_delegationpackage_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditdelegationpackage (
id,assignmentpackageid,delegationid,packageid,rolepackageid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.assignmentpackageid,OLD.delegationid,OLD.packageid,OLD.rolepackageid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_delegationpackage_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditdelegationpackage (
id,assignmentpackageid,delegationid,packageid,rolepackageid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.assignmentpackageid,OLD.delegationid,OLD.packageid,OLD.rolepackageid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

alter table dbo.delegationresource alter column audit_changedby drop not null;
alter table dbo.delegationresource alter column audit_changedbysystem drop not null;
alter table dbo.delegationresource alter column audit_changeoperation drop not null;
alter table dbo.delegationresource alter column audit_validfrom drop default;
alter table dbo.delegationresource drop constraint uc_delegationresource_delegationid_resourceid;

ALTER TABLE dbo.delegationresource RENAME CONSTRAINT fk_delegationresource_delegation_delegation TO fk_delegationresource_delegation_delegationid;
ALTER TABLE dbo.delegationresource DROP CONSTRAINT fk_delegationresource_resource_resource;

alter table dbo.delegationresource add constraint fk_delegationresource_resource_resourceid foreign key (resourceid) references dbo.resource on delete restrict;

ALTER INDEX dbo.uc_delegationresource_delegationid_resourceid_idx RENAME TO ix_delegationresource_delegationid_resourceid;
ALTER INDEX dbo.fk_delegationresource_delegationid_delegation_idx RENAME TO ix_delegationresource_delegationid;
ALTER INDEX dbo.fk_delegationresource_resourceid_resource_idx RENAME TO ix_delegationresource_resourceid;

ALTER TRIGGER delegationresource_audit_update ON dbo.delegationresource RENAME TO audit_delegationresource_update_trg;
ALTER TRIGGER delegationresource_audit_delete ON dbo.delegationresource RENAME TO audit_delegationresource_delete_trg;
drop trigger delegationresource_meta on dbo.delegationresource;

create or replace function dbo.audit_delegationresource_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditdelegationresource (
id,delegationid,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.delegationid,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_delegationresource_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditdelegationresource (
id,delegationid,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.delegationid,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

create or replace function dbo.audit_delegationresource_insert_fn() returns trigger
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

create or replace trigger audit_delegationresource_insert_trg
    before insert or update
    on dbo.delegationresource
    for each row
execute procedure dbo.audit_delegationresource_insert_fn();

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

alter table dbo.entitylookup alter column audit_changedby drop not null;
alter table dbo.entitylookup alter column audit_changedbysystem drop not null;
alter table dbo.entitylookup alter column audit_changeoperation drop not null;
alter table dbo.entitylookup alter column audit_validfrom drop default;

alter table dbo.entitylookup drop constraint uc_entitylookup_entityid_key;
ALTER TABLE dbo.entitylookup RENAME CONSTRAINT fk_entitylookup_entity_entity TO fk_entitylookup_entity_entityid;

ALTER INDEX dbo.fk_entitylookup_entityid_entity_idx RENAME TO ix_entitylookup_entityid;
ALTER INDEX dbo.uc_entitylookup_entityid_key_idx RENAME TO ix_entitylookup_entityid_key;
drop index dbo.entitylookup_key_value_idx;

ALTER TRIGGER entitylookup_audit_update ON dbo.entitylookup RENAME TO audit_entitylookup_update_trg;
ALTER TRIGGER entitylookup_audit_delete ON dbo.entitylookup RENAME TO audit_entitylookup_delete_trg;
drop trigger entitylookup_meta on dbo.entitylookup;

create or replace function dbo.audit_entitytype_insert_fn() returns trigger
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

create or replace function dbo.audit_entitylookup_insert_fn() returns trigger
    language plpgsql
as
$$
BEGIN
NEW.audit_validfrom := now();
RETURN NEW;
END;
$$;

create or replace trigger audit_entitylookup_insert_trg
    before insert or update
    on dbo.entitylookup
    for each row
execute procedure dbo.audit_entitylookup_insert_fn();

create or replace function dbo.audit_entitylookup_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditentitylookup (
id,entityid,isprotected,key,value,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.entityid,OLD.isprotected,OLD.key,OLD.value,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_entitylookup_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditentitylookup (
id,entityid,isprotected,key,value,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.entityid,OLD.isprotected,OLD.key,OLD.value,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

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

alter table dbo.package alter column audit_changedby drop not null;
alter table dbo.package alter column audit_changedbysystem drop not null;
alter table dbo.package alter column audit_changeoperation drop not null;
alter table dbo.package alter column audit_validfrom drop default;

alter table dbo.package alter column name drop not null;
alter table dbo.package alter column description drop not null;
alter table dbo.package alter column urn drop not null;

alter table dbo.package drop constraint uc_package_providerid_name;

ALTER TABLE dbo.package DROP CONSTRAINT fk_package_provider_provider;
ALTER TABLE dbo.package DROP CONSTRAINT fk_package_entitytype_entitytype;
ALTER TABLE dbo.package DROP CONSTRAINT fk_package_area_area;

alter table dbo.package add constraint fk_package_area_areaid foreign key (areaid) references dbo.area on delete restrict;
alter table dbo.package add constraint fk_package_entitytype_entitytypeid foreign key (entitytypeid) references dbo.entitytype on delete restrict;
alter table dbo.package add constraint fk_package_provider_providerid foreign key (providerid) references dbo.provider on delete restrict;

ALTER INDEX dbo.uc_package_providerid_name_idx RENAME TO ix_package_providerid_name;

ALTER INDEX dbo.fk_package_areaid_area_idx RENAME TO ix_package_areaid;
ALTER INDEX dbo.fk_package_entitytypeid_entitytype_idx RENAME TO ix_package_entitytypeid;
ALTER INDEX dbo.fk_package_providerid_provider_idx RENAME TO ix_package_providerid;

ALTER TRIGGER package_audit_update ON dbo.package RENAME TO audit_package_update_trg;
ALTER TRIGGER package_audit_delete ON dbo.package RENAME TO audit_package_delete_trg;
drop trigger package_meta on dbo.package;

create or replace function dbo.audit_package_insert_fn() returns trigger
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

create or replace trigger audit_package_insert_trg
    before insert or update
    on dbo.package
    for each row
execute procedure dbo.audit_package_insert_fn();

create or replace function dbo.audit_package_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditpackage (
id,areaid,description,entitytypeid,hasresources,isassignable,isdelegable,name,providerid,urn,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.areaid,OLD.description,OLD.entitytypeid,OLD.hasresources,OLD.isassignable,OLD.isdelegable,OLD.name,OLD.providerid,OLD.urn,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_package_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditpackage (
id,areaid,description,entitytypeid,hasresources,isassignable,isdelegable,name,providerid,urn,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.areaid,OLD.description,OLD.entitytypeid,OLD.hasresources,OLD.isassignable,OLD.isdelegable,OLD.name,OLD.providerid,OLD.urn,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

alter table dbo.packageresource alter column audit_changedby drop not null;
alter table dbo.packageresource alter column audit_changedbysystem drop not null;
alter table dbo.packageresource alter column audit_changeoperation drop not null;
alter table dbo.packageresource alter column audit_validfrom drop default;

alter table dbo.packageresource drop constraint uc_packageresource_packageid_resourceid;
ALTER TABLE dbo.packageresource RENAME CONSTRAINT fk_packageresource_package_package TO fk_packageresource_package_packageid;
ALTER TABLE dbo.packageresource drop CONSTRAINT fk_packageresource_resource_resource;

alter table dbo.packageresource add constraint fk_packageresource_resource_resourceid foreign key (resourceid) references dbo.resource on delete restrict;

ALTER INDEX dbo.fk_packageresource_packageid_package_idx RENAME TO ix_packageresource_packageid;
ALTER INDEX dbo.fk_packageresource_resourceid_resource_idx RENAME TO ix_packageresource_resourceid;
ALTER INDEX dbo.uc_packageresource_packageid_resourceid_idx RENAME TO ix_packageresource_packageid_resourceid;

ALTER TRIGGER packageresource_audit_update ON dbo.packageresource RENAME TO audit_packageresource_update_trg;
ALTER TRIGGER packageresource_audit_delete ON dbo.packageresource RENAME TO audit_packageresource_delete_trg;
drop trigger packageresource_meta on dbo.packageresource;

create or replace function dbo.audit_packageresource_insert_fn() returns trigger
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

create or replace trigger audit_packageresource_insert_trg
    before insert or update
    on dbo.packageresource
    for each row
execute procedure dbo.audit_packageresource_insert_fn();

create or replace function dbo.audit_packageresource_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditpackageresource (
id,packageid,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.packageid,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_packageresource_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditpackageresource (
id,packageid,resourceid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.packageid,OLD.resourceid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

alter table dbo.provider alter column audit_changedby drop not null;
alter table dbo.provider alter column audit_changedbysystem drop not null;
alter table dbo.provider alter column audit_changeoperation drop not null;
alter table dbo.provider alter column audit_validfrom drop default;

alter table dbo.provider alter column typeid set not null;
alter table dbo.provider drop constraint uc_provider_name;

ALTER TABLE dbo.provider DROP CONSTRAINT fk_provider_type_providertype;
alter table dbo.provider add constraint fk_provider_providertype_typeid foreign key (typeid) references dbo.providertype on delete restrict;

ALTER INDEX dbo.uc_provider_name_idx RENAME TO ix_provider_name;
ALTER INDEX dbo.fk_provider_typeid_providertype_idx RENAME TO ix_provider_typeid;

ALTER TRIGGER provider_audit_update ON dbo.provider RENAME TO audit_provider_update_trg;
ALTER TRIGGER provider_audit_delete ON dbo.provider RENAME TO audit_provider_delete_trg;
drop trigger provider_meta on dbo.provider;

create or replace function dbo.audit_provider_insert_fn() returns trigger
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

create or replace trigger audit_provider_insert_trg
    before insert or update
    on dbo.provider
    for each row
execute procedure dbo.audit_provider_insert_fn();

create or replace function dbo.audit_provider_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditprovider (
id,code,logourl,name,refid,typeid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.code,OLD.logourl,OLD.name,OLD.refid,OLD.typeid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_provider_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditprovider (
id,code,logourl,name,refid,typeid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.code,OLD.logourl,OLD.name,OLD.refid,OLD.typeid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

alter table dbo.providertype alter column audit_changedby drop not null;
alter table dbo.providertype alter column audit_changedbysystem drop not null;
alter table dbo.providertype alter column audit_changeoperation drop not null;
alter table dbo.providertype alter column audit_validfrom drop default;

alter table dbo.providertype drop constraint uc_providertype_name;

ALTER INDEX dbo.uc_providertype_name_idx RENAME TO ix_providertype_name;

ALTER TRIGGER providertype_audit_update ON dbo.providertype RENAME TO audit_providertype_update_trg;
ALTER TRIGGER providertype_audit_delete ON dbo.providertype RENAME TO audit_providertype_delete_trg;
drop trigger providertype_meta on dbo.providertype;

create or replace function dbo.audit_providertype_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditprovidertype (
id,name,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.name,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_providertype_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditprovidertype (
id,name,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.name,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

create or replace function dbo.audit_providertype_insert_fn() returns trigger
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

create or replace trigger audit_providertype_insert_trg
    before insert or update
    on dbo.providertype
    for each row
execute procedure dbo.audit_providertype_insert_fn();

alter table dbo.resource alter column audit_changedby drop not null;\
alter table dbo.resource alter column audit_changedbysystem drop not null;
alter table dbo.resource alter column audit_changeoperation drop not null;
alter table dbo.resource alter column audit_validfrom drop default;
alter table dbo.resource alter column refid drop not null;

alter table dbo.resource drop constraint uc_resource_providerid_refid;
ALTER TABLE dbo.resource drop CONSTRAINT fk_resource_provider_provider;
ALTER TABLE dbo.resource drop CONSTRAINT fk_resource_type_resourcetype;

alter table dbo.resource add constraint fk_resource_provider_providerid foreign key (providerid) references dbo.provider on delete restrict;
alter table dbo.resource add constraint fk_resource_resourcetype_typeid foreign key (typeid) references dbo.resourcetype on delete restrict;

ALTER INDEX dbo.fk_resource_providerid_provider_idx RENAME TO ix_resource_providerid;
ALTER INDEX dbo.fk_resource_typeid_resourcetype_idx RENAME TO ix_resource_typeid;
drop index dbo.uc_resource_providerid_refid_idx;

ALTER TRIGGER resource_audit_update ON dbo.resource RENAME TO audit_resource_update_trg;
ALTER TRIGGER resource_audit_delete ON dbo.resource RENAME TO audit_resource_delete_trg;
drop trigger resource_meta on dbo.resource;

create or replace function dbo.audit_resource_insert_fn() returns trigger
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

create or replace trigger audit_resource_insert_trg
    before insert or update
    on dbo.resource
    for each row
execute procedure dbo.audit_resource_insert_fn();

create or replace function dbo.audit_resource_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditresource (
id,description,name,providerid,refid,typeid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.description,OLD.name,OLD.providerid,OLD.refid,OLD.typeid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_resource_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditresource (
id,description,name,providerid,refid,typeid,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.description,OLD.name,OLD.providerid,OLD.refid,OLD.typeid,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

alter table dbo.resourcetype alter column audit_changedby drop not null;
alter table dbo.resourcetype alter column audit_changedbysystem drop not null;
alter table dbo.resourcetype alter column audit_changeoperation drop not null;
alter table dbo.resourcetype alter column audit_validfrom drop default;

alter table dbo.resourcetype drop constraint uc_resourcetype_name;
DROP INDEX dbo.uc_resourcetype_name_idx;
CREATE UNIQUE INDEX ix_resourcetype_name ON dbo.resourcetype (name);

ALTER TRIGGER resourcetype_audit_update ON dbo.resourcetype RENAME TO audit_resourcetype_update_trg;
ALTER TRIGGER resourcetype_audit_delete ON dbo.resourcetype RENAME TO audit_resourcetype_delete_trg;
drop trigger resourcetype_meta on dbo.resourcetype;

create or replace function dbo.audit_resourcetype_insert_fn() returns trigger
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

create or replace trigger audit_resourcetype_insert_trg
    before insert or update
    on dbo.resourcetype
    for each row
execute procedure dbo.audit_resourcetype_insert_fn();

create or replace function dbo.audit_resourcetype_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditresourcetype (
id,name,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.name,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_resourcetype_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditresourcetype (
id,name,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.name,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

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

create or replace trigger audit_role_insert_trg
    before insert or update
    on dbo.role
    for each row
execute procedure dbo.audit_role_insert_fn();

alter table dbo.rolelookup alter column audit_changedby drop not null;
alter table dbo.rolelookup alter column audit_changedbysystem drop not null;
alter table dbo.rolelookup alter column audit_changeoperation drop not null;
alter table dbo.rolelookup alter column audit_validfrom drop default;

alter table dbo.rolelookup drop constraint uc_rolelookup_roleid_key;
ALTER TABLE dbo.rolelookup RENAME CONSTRAINT fk_rolelookup_role_role TO fk_rolelookup_role_roleid;

ALTER INDEX dbo.fk_rolelookup_roleid_role_idx RENAME TO ix_rolelookup_roleid;
ALTER INDEX dbo.uc_rolelookup_roleid_key_idx RENAME TO ix_rolelookup_roleid_key;

ALTER TRIGGER rolelookup_audit_update ON dbo.rolelookup RENAME TO audit_rolelookup_update_trg;
ALTER TRIGGER rolelookup_audit_delete ON dbo.rolelookup RENAME TO audit_rolelookup_delete_trg;
drop trigger rolelookup_meta on dbo.rolelookup;

create or replace function dbo.audit_rolelookup_insert_fn() returns trigger
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

create or replace trigger audit_rolelookup_insert_trg
    before insert or update
    on dbo.rolelookup
    for each row
execute procedure dbo.audit_rolelookup_insert_fn();

create or replace function dbo.audit_rolelookup_update_fn() returns trigger
    language plpgsql
as
$$
BEGIN
INSERT INTO dbo_history.auditrolelookup (
id,key,roleid,value,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation
) VALUES (
OLD.id,OLD.key,OLD.roleid,OLD.value,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation
);
RETURN NEW;
END;
$$;

create or replace function dbo.audit_rolelookup_delete_fn() returns trigger
    language plpgsql
as
$$
DECLARE ctx RECORD;
BEGIN
SELECT * INTO ctx FROM session_audit_context LIMIT 1;
INSERT INTO dbo_history.auditrolelookup (
id,key,roleid,value,
audit_validfrom, audit_validto,
audit_changedby, audit_changedbysystem, audit_changeoperation,
audit_deletedby, audit_deletedbysystem, audit_deleteoperation
) VALUES (
OLD.id,OLD.key,OLD.roleid,OLD.value,
OLD.audit_validfrom, now(),
OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,
ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id
);
RETURN OLD;
END;
$$;

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

create table dbo.translationentry
(
    id           uuid not null,
    type         text not null,
    languagecode text not null,
    fieldname    text not null,
    value        text,
    constraint pk_translationentry
        primary key (id, type, languagecode, fieldname)
);

drop trigger statusrecord_meta on dbo.statusrecord;
drop trigger statusrecord_audit_update on dbo.statusrecord;
drop trigger statusrecord_audit_delete on dbo.statusrecord;

drop function dbo.audit_statusrecord_update_fn();
drop function dbo.audit_statusrecord_delete_fn();

