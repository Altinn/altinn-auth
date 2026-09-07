START TRANSACTION;
DROP TRIGGER IF EXISTS activitylog_assignment_insert_trg ON dbo.assignment;
DROP TRIGGER IF EXISTS activitylog_assignment_delete_trg ON dbo.assignment;
DROP TRIGGER IF EXISTS activitylog_assignmentpackage_insert_trg ON dbo.assignmentpackage;
DROP TRIGGER IF EXISTS activitylog_assignmentpackage_delete_trg ON dbo.assignmentpackage;
DROP TRIGGER IF EXISTS activitylog_assignmentresource_insert_trg ON dbo.assignmentresource;
DROP TRIGGER IF EXISTS activitylog_assignmentresource_delete_trg ON dbo.assignmentresource;
DROP TRIGGER IF EXISTS activitylog_assignmentinstance_insert_trg ON dbo.assignmentinstance;
DROP TRIGGER IF EXISTS activitylog_assignmentinstance_update_trg ON dbo.assignmentinstance;
DROP TRIGGER IF EXISTS activitylog_assignmentinstance_delete_trg ON dbo.assignmentinstance;
DROP TRIGGER IF EXISTS activitylog_delegation_insert_trg ON dbo.delegation;
DROP TRIGGER IF EXISTS activitylog_delegation_delete_trg ON dbo.delegation;
DROP TRIGGER IF EXISTS activitylog_delegationpackage_insert_trg ON dbo.delegationpackage;
DROP TRIGGER IF EXISTS activitylog_delegationpackage_delete_trg ON dbo.delegationpackage;
DROP TRIGGER IF EXISTS activitylog_delegationresource_insert_trg ON dbo.delegationresource;
DROP TRIGGER IF EXISTS activitylog_delegationresource_delete_trg ON dbo.delegationresource;
DROP TRIGGER IF EXISTS activitylog_requestassignment_insert_trg ON dbo.requestassignment;
DROP TRIGGER IF EXISTS activitylog_requestassignment_delete_trg ON dbo.requestassignment;
DROP TRIGGER IF EXISTS activitylog_requestassignmentpackage_insert_trg ON dbo.requestassignmentpackage;
DROP TRIGGER IF EXISTS activitylog_requestassignmentpackage_update_trg ON dbo.requestassignmentpackage;
DROP TRIGGER IF EXISTS activitylog_requestassignmentpackage_delete_trg ON dbo.requestassignmentpackage;
DROP TRIGGER IF EXISTS activitylog_requestassignmentresource_insert_trg ON dbo.requestassignmentresource;
DROP TRIGGER IF EXISTS activitylog_requestassignmentresource_update_trg ON dbo.requestassignmentresource;
DROP TRIGGER IF EXISTS activitylog_requestassignmentresource_delete_trg ON dbo.requestassignmentresource;

DROP FUNCTION IF EXISTS dbo.activitylog_assignment_insert_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_assignment_delete_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_assignmentpackage_insert_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_assignmentpackage_delete_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_assignmentresource_insert_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_assignmentresource_delete_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_assignmentinstance_insert_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_assignmentinstance_update_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_assignmentinstance_delete_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_delegation_insert_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_delegation_delete_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_delegationpackage_insert_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_delegationpackage_delete_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_delegationresource_insert_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_delegationresource_delete_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_requestassignment_insert_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_requestassignment_delete_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_requestassignmentpackage_insert_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_requestassignmentpackage_update_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_requestassignmentpackage_delete_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_requestassignmentresource_insert_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_requestassignmentresource_update_fn();
DROP FUNCTION IF EXISTS dbo.activitylog_requestassignmentresource_delete_fn();

DROP FUNCTION IF EXISTS dbo.activitylog_ensure_month_partitions(int);
DROP FUNCTION IF EXISTS dbo.activitylog_ensure_partitions(date, date);
DROP FUNCTION IF EXISTS dbo.activitylog_requestassignment_info(uuid);
DROP FUNCTION IF EXISTS dbo.activitylog_delegation_info(uuid);
DROP FUNCTION IF EXISTS dbo.activitylog_assignment_info(uuid);
DROP FUNCTION IF EXISTS dbo.activitylog_resource_name(uuid);
DROP FUNCTION IF EXISTS dbo.activitylog_package_name(uuid);
DROP FUNCTION IF EXISTS dbo.activitylog_role_name(uuid);
DROP FUNCTION IF EXISTS dbo.activitylog_entity_name(uuid);
DROP FUNCTION IF EXISTS dbo.activitylog_entity_info(uuid);

DROP TABLE dbo.activitylog;

DROP TABLE dbo.activitylogbackfillprogress;

DROP FUNCTION IF EXISTS dbo.uuid_generate_v7();


COMMIT;

