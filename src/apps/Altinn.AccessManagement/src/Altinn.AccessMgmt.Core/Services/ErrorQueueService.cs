using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services
{
    public class ErrorQueueService(AppDbContext db) : IErrorQueueService
    {
        /// <inheritdoc />
        public async Task<bool> AddErrorQueue(ErrorQueue error, AuditValues values, CancellationToken cancellationToken)
        {
            await db.ErrorQueue.AddAsync(error, cancellationToken);
            var affected = await db.SaveChangesAsync(values, cancellationToken);
            return affected != 0;
        }

        /// <inheritdoc />
        public async Task<List<ErrorQueue>> RetrieveItemsForReProcessing(string type, CancellationToken cancellationToken)
        {
            var items = await db.ErrorQueue.AsNoTracking()
            .Where(t => t.OriginType == type && t.ReProcess && !t.Processed)
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);
            
            return items;
        }

        /// <inheritdoc />
        public async Task<bool> MarkErrorQueueElementProcessed(Guid id, AuditValues values, CancellationToken cancellationToken)
        {
            var res = await db.ErrorQueue.AsTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (res != null)
            {
                res.Processed = true;
            }

            var result = await db.SaveChangesAsync(values, cancellationToken);

            return result > 0;            
        }

        /// <inheritdoc />
        public async Task<bool> UpdateErrorMessage(Guid id, AuditValues values, string errorMessage, CancellationToken cancellationToken)
        {
            var res = await db.ErrorQueue.AsTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (res != null)
            {
                res.ErrorMessage = errorMessage;
            }

            var result = await db.SaveChangesAsync(values, cancellationToken);

            return result > 0;
        }
    }
}
