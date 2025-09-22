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
