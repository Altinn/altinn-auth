DROP VIEW dbo_history.delegationresource;
DROP VIEW dbo_history.providertype;
DROP VIEW dbo_history.resourcetype;
DROP VIEW dbo_history.provider;
DROP VIEW dbo_history.resource;
DROP VIEW dbo_history.entitytype;
DROP VIEW dbo_history.statusrecord;
DROP VIEW dbo_history.entityvariant;
DROP VIEW dbo_history.areagroup;
DROP VIEW dbo_history.area;
DROP VIEW dbo_history.role;
DROP VIEW dbo_history.entityvariantrole;
DROP VIEW dbo_history.entity;
DROP VIEW dbo_history.package;
DROP VIEW dbo_history.assignment;
DROP VIEW dbo_history.rolelookup;
DROP VIEW dbo_history.rolemap;
DROP VIEW dbo_history.entitylookup;
DROP VIEW dbo_history.roleresource;
DROP VIEW dbo_history.rolepackage;
DROP VIEW dbo_history.assignmentresource;
DROP VIEW dbo_history.packageresource;
DROP VIEW dbo_history.delegation;
DROP VIEW dbo_history.assignmentpackage;
DROP VIEW dbo_history.delegationpackage;

ALTER TABLE dbo_history._delegationresource RENAME TO auditdelegationresource;
ALTER TABLE dbo_history._providertype RENAME TO auditprovidertype;
ALTER TABLE dbo_history._resourcetype RENAME TO auditresourcetype;
ALTER TABLE dbo_history._provider RENAME TO auditprovider;
ALTER TABLE dbo_history._resource RENAME TO auditresource;
ALTER TABLE dbo_history._entitytype RENAME TO auditentitytype;
ALTER TABLE dbo_history._statusrecord RENAME TO auditstatusrecord;
ALTER TABLE dbo_history._entityvariant RENAME TO auditentityvariant;
ALTER TABLE dbo_history._areagroup RENAME TO auditareagroup;
ALTER TABLE dbo_history._area RENAME TO auditarea;
ALTER TABLE dbo_history._role RENAME TO auditrole;
ALTER TABLE dbo_history._entityvariantrole RENAME TO auditentityvariantrole;
ALTER TABLE dbo_history._entity RENAME TO auditentity;
ALTER TABLE dbo_history._package RENAME TO auditpackage;
ALTER TABLE dbo_history._assignment RENAME TO auditassignment;
ALTER TABLE dbo_history._rolelookup RENAME TO auditrolelookup;
ALTER TABLE dbo_history._rolemap RENAME TO auditrolemap;
ALTER TABLE dbo_history._entitylookup RENAME TO auditentitylookup;
ALTER TABLE dbo_history._roleresource RENAME TO auditroleresource;
ALTER TABLE dbo_history._rolepackage RENAME TO auditrolepackage;
ALTER TABLE dbo_history._assignmentresource RENAME TO auditassignmentresource;
ALTER TABLE dbo_history._packageresource RENAME TO auditpackageresource;
ALTER TABLE dbo_history._delegation RENAME TO auditdelegation;
ALTER TABLE dbo_history._assignmentpackage RENAME TO auditassignmentpackage;
ALTER TABLE dbo_history._delegationpackage RENAME TO auditdelegationpackage;

alter table dbo_history.auditarea
    add constraint pk_auditarea
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditareagroup
    add constraint pk_auditareagroup
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditassignment
    add constraint pk_auditassignment
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditassignmentpackage
    add constraint pk_auditassignmentpackage
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditassignmentresource
    add constraint pk_auditassignmentresource
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditdelegation
    add constraint pk_auditdelegation
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditdelegationpackage
    add constraint pk_auditdelegationpackage
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditdelegationresource
    add constraint pk_auditdelegationresource
        primary key (id, audit_validfrom, audit_validto);

--alter table dbo_history.auditentity
--    add constraint pk_auditentity
--        primary key (id, audit_validfrom, audit_validto);

--alter table dbo_history.auditentitylookup
--    add constraint pk_auditentitylookup
--        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditentitytype
    add constraint pk_auditentitytype
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditentityvariant
    add constraint pk_auditentityvariant
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditentityvariantrole
    add constraint pk_auditentityvariantrole
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditpackage
    add constraint pk_auditpackage
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditpackageresource
    add constraint pk_auditpackageresource
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditprovider
    add constraint pk_auditprovider
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditprovidertype
    add constraint pk_auditprovidertype
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditresource
    add constraint pk_auditresource
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditresourcetype
    add constraint pk_auditresourcetype
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditrole
    add constraint pk_auditrole
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditrolelookup
    add constraint pk_auditrolelookup
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditrolemap
    add constraint pk_auditrolemap
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditrolepackage
    add constraint pk_auditrolepackage
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditroleresource
    add constraint pk_auditroleresource
        primary key (id, audit_validfrom, audit_validto);

alter table dbo_history.auditprovidertype
    alter column audit_changedby drop not null;

alter table dbo_history.auditprovidertype
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditprovidertype
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditresourcetype
    alter column audit_changedby drop not null;

alter table dbo_history.auditresourcetype
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditresourcetype
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditprovider
    alter column audit_changedby drop not null;

alter table dbo_history.auditprovider
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditprovider
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditprovider
    alter column typeid set not null;

alter table dbo_history.auditentitytype
    alter column audit_changedby drop not null,
    alter column audit_changedbysystem drop not null,
    alter column audit_changeoperation drop not null,
    alter column providerid set not null;

alter table dbo_history.auditresource
    alter column audit_changedby drop not null;

alter table dbo_history.auditresource
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditresource
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditresource
    alter column providerid set not null;

alter table dbo_history.auditresource
    alter column typeid set not null;

alter table dbo_history.auditentityvariant
    alter column audit_changedby drop not null,
    alter column audit_changedbysystem drop not null,
    alter column audit_changeoperation drop not null,
    alter column typeid set not null;

alter table dbo_history.auditareagroup
    alter column audit_changedby drop not null;

alter table dbo_history.auditareagroup
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditareagroup
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditareagroup
    alter column entitytypeid set not null;

alter table dbo_history.auditarea
    alter column audit_changedby drop not null;

alter table dbo_history.auditarea
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditarea
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditarea
    alter column groupid set not null;

alter table dbo_history.auditrole
    alter column audit_changedby drop not null;

alter table dbo_history.auditrole
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditrole
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditrole
    alter column iskeyrole set not null;

alter table dbo_history.auditrole
    alter column iskeyrole drop default;

alter table dbo_history.auditrole
    alter column isassignable set not null;

alter table dbo_history.auditrole
    alter column isassignable drop default;

alter table dbo_history.auditrole
    alter column providerid set not null;

alter table dbo_history.auditentityvariantrole
    alter column audit_changedby drop not null,
    alter column audit_changedbysystem drop not null,
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditentityvariantrole
    alter column variantid set not null,
    alter column roleid set not null;

alter table dbo_history.auditentity
    alter column audit_changedby drop not null,
    alter column audit_changedbysystem drop not null,
    alter column audit_changeoperation drop not null,
    alter column typeid set not null,
    alter column variantid set not null;

alter table dbo_history.auditpackage
    alter column audit_changedby drop not null;

alter table dbo_history.auditpackage
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditpackage
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditpackage
    alter column isassignable set not null,
    alter column isassignable drop default,
    alter column isdelegable set not null,
    alter column isdelegable drop default,
    alter column hasresources set not null,
    alter column providerid set not null,
    alter column entitytypeid set not null,
    alter column areaid set not null;

alter table dbo_history.auditassignment
    alter column audit_changedby drop not null,
    alter column audit_changedbysystem drop not null,
    alter column audit_changeoperation drop not null,
    alter column roleid set not null,
    alter column fromid set not null,
    alter column toid set not null;

alter table dbo_history.auditrolelookup
    alter column audit_changedby drop not null,
    alter column audit_changedbysystem drop not null,
    alter column audit_changeoperation drop not null,
    alter column roleid set not null;

alter table dbo_history.auditrolemap
    alter column audit_changedby drop not null,
    alter column audit_changedbysystem drop not null,
    alter column audit_changeoperation drop not null,
    alter column hasroleid set not null,
    alter column getroleid set not null;

--alter table dbo_history.auditentitylookup
--    alter column audit_changedby drop not null;
--
--alter table dbo_history.auditentitylookup
--    alter column audit_changedbysystem drop not null;
--
--alter table dbo_history.auditentitylookup
--    alter column audit_changeoperation drop not null;
--
--alter table dbo_history.auditentitylookup
--    alter column entityid set not null;
--
--alter table dbo_history.auditentitylookup
--    alter column isprotected set not null;
--
--alter table dbo_history.auditentitylookup
--    alter column isprotected drop default;

alter table dbo_history.auditroleresource
    alter column audit_changedby drop not null;

alter table dbo_history.auditroleresource
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditroleresource
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditroleresource
    alter column roleid set not null;

alter table dbo_history.auditroleresource
    alter column resourceid set not null;

alter table dbo_history.auditrolepackage
    alter column audit_changedby drop not null;

alter table dbo_history.auditrolepackage
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditrolepackage
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditrolepackage
    alter column roleid set not null;

alter table dbo_history.auditrolepackage
    alter column packageid set not null;

alter table dbo_history.auditrolepackage
    alter column hasaccess set not null;

alter table dbo_history.auditrolepackage
    alter column candelegate set not null;

alter table dbo_history.auditassignmentresource
    alter column audit_changedby drop not null;

alter table dbo_history.auditassignmentresource
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditassignmentresource
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditassignmentresource
    alter column assignmentid set not null;

alter table dbo_history.auditassignmentresource
    alter column resourceid set not null;

alter table dbo_history.auditdelegation
    alter column audit_changedby drop not null;

alter table dbo_history.auditdelegation
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditdelegation
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditdelegation
    alter column fromid set not null;

alter table dbo_history.auditdelegation
    alter column toid set not null;

alter table dbo_history.auditdelegation
    alter column facilitatorid set not null;

alter table dbo_history.auditpackageresource
    alter column audit_changedby drop not null;

alter table dbo_history.auditpackageresource
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditpackageresource
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditpackageresource
    alter column packageid set not null;

alter table dbo_history.auditpackageresource
    alter column resourceid set not null;

alter table dbo_history.auditassignmentpackage
    alter column audit_changedby drop not null;

alter table dbo_history.auditassignmentpackage
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditassignmentpackage
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditassignmentpackage
    alter column assignmentid set not null;

alter table dbo_history.auditassignmentpackage
    alter column packageid set not null;

alter table dbo_history.auditdelegationpackage
    alter column audit_changedby drop not null;

alter table dbo_history.auditdelegationpackage
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditdelegationpackage
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditdelegationpackage
    alter column delegationid set not null;

alter table dbo_history.auditdelegationpackage
    alter column packageid set not null;

alter table dbo_history.auditdelegationresource
    alter column audit_changedby drop not null;

alter table dbo_history.auditdelegationresource
    alter column audit_changedbysystem drop not null;

alter table dbo_history.auditdelegationresource
    alter column audit_changeoperation drop not null;

alter table dbo_history.auditdelegationresource
    alter column delegationid set not null;

alter table dbo_history.auditdelegationresource
    alter column resourceid set not null;
