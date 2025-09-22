drop trigger statusrecord_meta on dbo.statusrecord;
drop trigger statusrecord_audit_update on dbo.statusrecord;
drop trigger statusrecord_audit_delete on dbo.statusrecord;

drop function dbo.audit_statusrecord_update_fn();
drop function dbo.audit_statusrecord_delete_fn();