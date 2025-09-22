create view dbo.connection(fromid, roleid, viaid, viaroleid, toid, packageid, resourceid, reason) as
SELECT a.fromid,
       a.roleid,
       NULL::uuid     AS viaid,
       NULL::uuid     AS viaroleid,
       a.toid,
       ap.packageid,
       NULL::uuid     AS resourceid,
       'Direct'::text AS reason
FROM assignment a
         JOIN assignmentpackage ap ON ap.assignmentid = a.id
UNION ALL
SELECT a.fromid,
       a.roleid,
       NULL::uuid     AS viaid,
       NULL::uuid     AS viaroleid,
       a.toid,
       rp.packageid,
       NULL::uuid     AS resourceid,
       'Direct'::text AS reason
FROM assignment a
         JOIN rolepackage rp ON rp.roleid = a.roleid AND rp.hasaccess = true
UNION ALL
SELECT a.fromid,
       a.roleid,
       NULL::uuid     AS viaid,
       NULL::uuid     AS viaroleid,
       a.toid,
       rp.packageid,
       NULL::uuid     AS resourceid,
       'Direct'::text AS reason
FROM assignment a
         JOIN rolemap rm ON a.roleid = rm.hasroleid
         JOIN rolepackage rp ON rp.roleid = rm.getroleid AND rp.hasaccess = true
UNION ALL
SELECT a.fromid,
       a.roleid,
       a.toid          AS viaid,
       a2.roleid       AS viaroleid,
       a2.toid,
       ap.packageid,
       NULL::uuid      AS resourceid,
       'KeyRole'::text AS reason
FROM assignment a
         JOIN assignment a2 ON a.toid = a2.fromid
         JOIN role r ON a2.roleid = r.id AND r.iskeyrole = true
         JOIN assignmentpackage ap ON ap.assignmentid = a.id
UNION ALL
SELECT a.fromid,
       a.roleid,
       a.toid          AS viaid,
       a2.roleid       AS viaroleid,
       a2.toid,
       rp.packageid,
       NULL::uuid      AS resourceid,
       'KeyRole'::text AS reason
FROM assignment a
         JOIN assignment a2 ON a.toid = a2.fromid
         JOIN role r ON a2.roleid = r.id AND r.iskeyrole = true
         JOIN rolepackage rp ON rp.roleid = a.roleid AND rp.hasaccess = true
UNION ALL
SELECT fa.fromid,
       fa.roleid,
       fa.toid            AS viaid,
       ta.roleid          AS viaroleid,
       ta.toid,
       dp.packageid,
       NULL::uuid         AS resourceid,
       'Delegation'::text AS reason
FROM delegation d
         JOIN assignment fa ON fa.id = d.fromid
         JOIN assignment ta ON ta.id = d.toid
         JOIN delegationpackage dp ON dp.delegationid = d.id;

