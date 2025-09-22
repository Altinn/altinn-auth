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
BEGIN
NEW.audit_validfrom := now();
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
