using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <inheritdoc />
public class StatusService(IStatusRecordRepository statusRecordRepository) : IStatusService
{
    private readonly IStatusRecordRepository statusRecordRepository = statusRecordRepository;

    /// <inheritdoc />
    public async Task<StatusRecord> GetOrCreateRecord(Guid id, string name, ChangeRequestOptions options, int limit = 5)
    {
        var status = await statusRecordRepository.Get(id);
        if (status == null)
        {
            status = new StatusRecord()
            {
                Name = name,
                RetryLimit = limit,
                RetryCount = 0,
                Message = "Initial",
                Payload = "[]",
                State = "RUNNING",
                Timestamp = DateTimeOffset.UtcNow
            };

            await statusRecordRepository.Upsert(status, options);
        }

        return status;
    }

    /// <inheritdoc />
    public async Task RunFailed(StatusRecord record, Exception exception, ChangeRequestOptions options)
    {
        record.State = "RETRY";
        record.Message = exception.Message;
        record.Payload = "[]";
        record.Timestamp = DateTimeOffset.UtcNow;
        await statusRecordRepository.Upsert(record, options);
    }

    /// <inheritdoc />
    public async Task RunSuccess(StatusRecord record, ChangeRequestOptions options)
    {
        if (record.State != "RUNNING" || record.Timestamp.AddMinutes(15) < DateTimeOffset.Now)
        {
            record.State = "RUNNING";
            record.RetryCount = 0;
            record.Message = "Ok";
            record.Payload = "[]";
            record.Timestamp = DateTimeOffset.UtcNow;
            await statusRecordRepository.Upsert(record, options);
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryToRun(StatusRecord record, ChangeRequestOptions options)
    {
        if (record.State == "STOPPED")
        {
            return false;
        }

        if (record.RetryCount >= record.RetryLimit)
        {
            record.State = "STOPPED";
            record.Timestamp = DateTimeOffset.UtcNow;
            await statusRecordRepository.Upsert(record, options);
            return false;
        }

        if (record.State == "RETRY")
        {
            record.RetryCount += 1;
        }

        record.Timestamp = DateTimeOffset.UtcNow;
        await statusRecordRepository.Upsert(record, options);

        return true;
    }
}

/// <summary>
/// Status service for jobs
/// </summary>
public interface IStatusService
{
    /// <summary>
    /// Get or create record
    /// </summary>
    /// <returns></returns>
    Task<StatusRecord> GetOrCreateRecord(Guid id, string name, ChangeRequestOptions options, int limit = 5);

    /// <summary>
    /// Try to run, checks state
    /// </summary>
    /// <returns></returns>
    Task<bool> TryToRun(StatusRecord record, ChangeRequestOptions options);

    /// <summary>
    /// Log run success
    /// </summary>
    /// <returns></returns>
    Task RunSuccess(StatusRecord record, ChangeRequestOptions options);

    /// <summary>
    /// Log run failed
    /// </summary>
    /// <returns></returns>
    Task RunFailed(StatusRecord record, Exception exception, ChangeRequestOptions options);
}
