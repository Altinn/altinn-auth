alter table dbo_history.auditentity
    add constraint pk_auditentity
        primary key (id, audit_validfrom, audit_validto);

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
    
alter table dbo.entity
    add constraint fk_entity_entity_parentid
        foreign key (parentid) references dbo.entity
            on delete restrict;

alter table dbo.entity
    add constraint fk_entity_entitytype_typeid
        foreign key (typeid) references dbo.entitytype
            on delete restrict;

alter table dbo.entity
    add constraint fk_entity_entityvariant_variantid
        foreign key (variantid) references dbo.entityvariant
            on delete restrict;