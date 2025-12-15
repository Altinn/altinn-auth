/*
//using Microsoft.EntityFrameworkCore.Migrations;

//#nullable disable

//namespace Altinn.AccessMgmt.PersistenceEF.Migrations
//{
//    /// <inheritdoc />
//    public partial class Functions : Migration
//    {
//        /// <inheritdoc />
//        protected override void Up(MigrationBuilder migrationBuilder)
//        {
//            migrationBuilder.Sql("""
//                CREATE FUNCTION entitychildren(_id uuid) returns jsonb stable language sql as
//                $$
//                SELECT jsonb_build_object(
//                    'Id', e.Id,
//                    'Name', e.Name
//                    )
//                FROM dbo.Entity e
//                WHERE e.ParentId = _id
//                GROUP BY e.Id
//                $$;
//                """);

//            migrationBuilder.Sql("""
//                create or replace function entityLookupValues(_id uuid) returns jsonb stable language sql as
//                $$
//                SELECT jsonb_object_agg(el.key, el.value)
//                FROM dbo.EntityLookup el
//                WHERE el.entityid = _id
//                $$;
//                """);

//            migrationBuilder.Sql("""
//                create or replace function roleLookupValues(_id uuid) returns jsonb stable language sql as
//                $$
//                SELECT jsonb_object_agg(rl.key, rl.value)
//                FROM dbo.RoleLookup rl
//                WHERE rl.roleid = _id
//                $$;
//                """);

//            migrationBuilder.Sql("""
//                create or replace function compactentity(_id uuid, _include_children boolean DEFAULT true, _include_lookups boolean DEFAULT true) returns jsonb stable language sql as
//                $$
//                SELECT jsonb_build_object(
//                    'Id', e.Id,
//                    'Name', e.Name,
//                    'Type', et.Name,
//                    'Variant', ev.Name,
//                    'Parent', compactentity(e.parentid, false, true),
//                    'Children', CASE WHEN _include_children THEN entitychildren(e.id) ELSE NULL END,
//                    'KeyValues', CASE WHEN _include_lookups THEN entitylookupvalues(e.id) ELSE NULL END
//                    )
//                FROM dbo.Entity e
//                JOIN dbo.EntityType et ON e.TypeId = et.Id
//                JOIN dbo.EntityVariant ev ON e.VariantId = ev.Id
//                LEFT OUTER JOIN dbo.Entity as ce on e.Id = ce.ParentId
//                LEFT OUTER JOIN dbo.EntityLookup as el on e.Id = el.entityid
//                WHERE e.Id = _Id
//                GROUP BY e.Id, e.Name, e.RefId, et.Name, ev.Name;
//                $$;

//                create or replace function entitychildren(_id uuid) returns jsonb stable language sql as
//                $$
//                SELECT COALESCE(json_agg(compactentity(e.Id, false, true)) FILTER (WHERE e.Id IS NOT NULL), NULL)
//                FROM dbo.Entity e
//                WHERE e.ParentId = _id
//                GROUP BY e.Id;
//                $$;
//                """);

//            migrationBuilder.Sql("""
//                create or replace function public.compactRole(_id uuid) returns jsonb stable language sql as
//                $$
//                SELECT jsonb_build_object(
//                    'Id', r.Id,
//                    'Code', r.Code,
//                    'Children', COALESCE(
//                                    json_agg(json_build_object('Id', rmr.Id, 'Value', rmr.Code, 'Children', null))
//                                    FILTER (WHERE rmr.Id IS NOT NULL), NULL)
//                )
//                FROM dbo.role r
//                left outer join dbo.RoleMap as rm on rm.HasRoleId = r.Id
//                left outer join dbo.Role as rmr on rm.GetRoleId = rmr.Id
//                WHERE r.id = _Id
//                group by r.Id, r.Name;
//                $$;
//                """);

//            migrationBuilder.Sql("""
//                create or replace function compactpackage(_id uuid) returns jsonb stable language sql as
//                $$
//                select jsonb_build_object('Id', p.Id,'Urn', p.Urn, 'AreaId', p.AreaId)
//                from dbo.Package as p
//                where p.id = _id;
//                $$;
//                """);

//            migrationBuilder.Sql("""
//                create or replace function compactresource(_id uuid) returns jsonb stable language sql as
//                $$
//                select jsonb_build_object('Id', r.Id,'Value', r.RefId)
//                from dbo.Resource as r
//                where r.id = _id;
//                $$;
//                """);

//            migrationBuilder.Sql("""
//                create or replace function namerole(_id uuid) returns text stable language sql as $$ select code from dbo.role where id = _id; $$;
//                create or replace function nameentity(_id uuid) returns text stable language sql as $$ select e.name || ' (' || ev.name || ')' from dbo.entity as e inner join dbo.entityvariant as ev on e.variantid = ev.id where e.id = _id; $$;
//                create or replace function namepackage(_id uuid) returns text stable language sql as $$ select name from dbo.package where id = _id; $$;
//                create or replace function nameassignment(_id uuid) returns text stable language sql as $$ select nameentity(a.fromid) || ' - ' || namerole(a.roleid) || ' - '  || nameentity(a.toid) from dbo.assignment as a where id = _id; $$;
//                create or replace function namedelegation(_id uuid) returns text stable language sql as $$ select nameassignment(d.fromid) || ' | ' || nameentity(d.facilitatorid) || ' | ' || nameassignment(d.toid) from dbo.delegation as d where id = _id; $$;
//                """);
//        }

//        /// <inheritdoc />
//        protected override void Down(MigrationBuilder migrationBuilder)
//        {
//            migrationBuilder.Sql("drop function entitychildren(_id uuid)");
//            migrationBuilder.Sql("drop function entityLookupValues(_id uuid)");
//            migrationBuilder.Sql("drop function roleLookupValues(_id uuid)");
//            migrationBuilder.Sql("drop function compactentity(_id uuid, _include_children boolean DEFAULT true, _include_lookups boolean DEFAULT true)");
//            migrationBuilder.Sql("drop function public.compactRole(_id uuid)");
//            migrationBuilder.Sql("drop function compactpackage(_id uuid);");
//            migrationBuilder.Sql("drop function compactresource(_id uuid)");
//            migrationBuilder.Sql("drop function namerole(_id uuid);");
//            migrationBuilder.Sql("drop function nameentity(_id uuid);");
//            migrationBuilder.Sql("drop function namepackage(_id uuid);");
//            migrationBuilder.Sql("drop function nameassignment(_id uuid);");
//            migrationBuilder.Sql("drop function namedelegation(_id uuid);");
//        }
//    }
//}
*/
