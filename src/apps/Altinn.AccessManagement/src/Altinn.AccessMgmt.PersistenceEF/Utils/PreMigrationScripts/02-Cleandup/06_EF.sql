create table public."__EFMigrationsHistory"
(
    "MigrationId"    varchar(150) not null constraint "PK___EFMigrationsHistory" primary key,
    "ProductVersion" varchar(32)  not null
);

alter table public."__EFMigrationsHistory"
    owner to platform_authorization_admin;

insert into public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
values ('20250922113124_Init','9.0.8'), ('20250922113202_Ingest','9.0.8'), ('20250922113248_ConnectionView','9.0.8');
