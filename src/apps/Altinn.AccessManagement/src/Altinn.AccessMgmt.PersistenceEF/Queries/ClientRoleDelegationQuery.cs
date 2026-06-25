using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Queries.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Altinn.AccessMgmt.PersistenceEF.Queries
{
    /// <summary>
    /// A static class that provides query methods for client role delegation operations.
    /// </summary>
    public static class ClientRoleDelegationQuery
    {
        /// <summary>
        /// Gets a list of client role delegations from the database context based on the specified parameters.
        /// </summary>
        /// <param name="dbContext">DbContext to use</param>
        /// <param name="fromId">the client the client role delegation is for</param>
        /// <param name="facilitatorId">the service unit performing the client delegation</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<ClientRoleAssignment>> GetClientRoleDelegations(
            this AppDbContext dbContext,
            Guid fromId,
            Guid facilitatorId,
            CancellationToken cancellationToken = default)
        {
            var rows = await dbContext.Database
               .SqlQueryRaw<ClientRoleAssignment>(
                   QUERY,
                   new NpgsqlParameter("facilitatorid", facilitatorId),
                   new NpgsqlParameter("fromid", fromId))

               .AsNoTracking()
               .ToListAsync(cancellationToken);

            return rows;
        }

        private static readonly string QUERY = /* sql */ """
            select
                a2c.id 
                ,a2c.fromid
                ,a2c.toid
                ,a2c.facilitatorid
                ,a2c.rolecode
                ,a2c.createddate
                ,a2c.performedby
                ,a.id assignmentid
            from 
                dbo.a2clientrole a2c
                join dbo."role" r on r.code = a2c.rolecode
                join dbo."assignment" a on a.fromid = a2c.fromid and a.toid = a2c.toid and a.roleid = r.id
            where
                a2c.facilitatorid = @facilitatorid
                and a2c.fromid  = @fromid
            """;
    }
}
