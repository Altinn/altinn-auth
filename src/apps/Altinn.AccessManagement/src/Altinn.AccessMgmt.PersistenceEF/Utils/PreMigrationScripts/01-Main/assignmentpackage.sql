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
BEGIN
NEW.audit_validfrom := now();
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
