alter table dbo_history.auditentitylookup
    add constraint pk_auditentitylookup
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditentitylookup
    alter column audit_changedby drop not null,
    alter column audit_changedbysystem drop not null,
    alter column audit_changeoperation drop not null,
    alter column entityid set not null,
    alter column isprotected set not null,
    alter column isprotected drop default;
