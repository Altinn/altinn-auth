using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services;
using Moq;

namespace AccessMgmt.Tests.Services;

/// <summary>
/// Unit tests for <see cref="StatusService"/> — pure Moq, no database.
/// </summary>
public class StatusServiceTest
{
    private static readonly ChangeRequestOptions DefaultOptions = new()
    {
        ChangedBy = Guid.NewGuid(),
        ChangedBySystem = Guid.NewGuid(),
    };

    private static (StatusService svc, Mock<IStatusRecordRepository> repo) MakeSut()
    {
        var repo = new Mock<IStatusRecordRepository>();
        return (new StatusService(repo.Object), repo);
    }

    private static StatusRecord MakeRecord(string state = "RUNNING", int retryCount = 0, int retryLimit = 3, DateTimeOffset? timestamp = null) =>
        new()
        {
            Name = "test-job",
            State = state,
            RetryCount = retryCount,
            RetryLimit = retryLimit,
            Message = "Ok",
            Payload = "[]",
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
        };

    #region GetOrCreateRecord

    [Fact]
    public async Task GetOrCreateRecord_ExistingRecord_ReturnsItWithoutUpsert()
    {
        var (svc, repo) = MakeSut();
        var existing = MakeRecord();
        var id = existing.Id;
        repo.Setup(r => r.Get(id, null, default, It.IsAny<string>())).ReturnsAsync(existing);

        var result = await svc.GetOrCreateRecord(id, "test-job", DefaultOptions);

        result.Should().BeSameAs(existing);
        repo.Verify(r => r.Upsert(It.IsAny<StatusRecord>(), It.IsAny<ChangeRequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateRecord_NoExistingRecord_CreatesNewAndCallsUpsert()
    {
        var (svc, repo) = MakeSut();
        var id = Guid.CreateVersion7();
        repo.Setup(r => r.Get(id, null, default, It.IsAny<string>())).ReturnsAsync((StatusRecord)null);
        repo.Setup(r => r.Upsert(It.IsAny<StatusRecord>(), DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>())).ReturnsAsync(1);

        var result = await svc.GetOrCreateRecord(id, "test-job", DefaultOptions, limit: 7);

        result.Should().NotBeNull();
        result.Name.Should().Be("test-job");
        result.RetryLimit.Should().Be(7);
        result.RetryCount.Should().Be(0);
        result.State.Should().Be("RUNNING");
        result.Message.Should().Be("Initial");
        repo.Verify(r => r.Upsert(result, DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateRecord_NoExistingRecord_DefaultLimitIs5()
    {
        var (svc, repo) = MakeSut();
        var id = Guid.CreateVersion7();
        repo.Setup(r => r.Get(id, null, default, It.IsAny<string>())).ReturnsAsync((StatusRecord)null);
        repo.Setup(r => r.Upsert(It.IsAny<StatusRecord>(), It.IsAny<ChangeRequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>())).ReturnsAsync(1);

        var result = await svc.GetOrCreateRecord(id, "job", DefaultOptions);

        result.RetryLimit.Should().Be(5);
    }

    #endregion

    #region RunFailed

    [Fact]
    public async Task RunFailed_SetsStateToRetryAndCallsUpsert()
    {
        var (svc, repo) = MakeSut();
        var record = MakeRecord(state: "RUNNING");
        var ex = new InvalidOperationException("boom");
        repo.Setup(r => r.Upsert(record, DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>())).ReturnsAsync(1);

        await svc.RunFailed(record, ex, DefaultOptions);

        record.State.Should().Be("RETRY");
        record.Message.Should().Be("boom");
        record.Payload.Should().Be("[]");
        repo.Verify(r => r.Upsert(record, DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RunFailed_TimestampIsUpdated()
    {
        var (svc, repo) = MakeSut();
        var record = MakeRecord(timestamp: DateTimeOffset.UtcNow.AddHours(-1));
        var before = DateTimeOffset.UtcNow;
        repo.Setup(r => r.Upsert(It.IsAny<StatusRecord>(), It.IsAny<ChangeRequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>())).ReturnsAsync(1);

        await svc.RunFailed(record, new Exception("err"), DefaultOptions);

        record.Timestamp.Should().BeOnOrAfter(before);
    }

    #endregion

    #region RunSuccess

    [Fact]
    public async Task RunSuccess_WhenStateIsNotRunning_UpdatesAndCallsUpsert()
    {
        var (svc, repo) = MakeSut();
        var record = MakeRecord(state: "RETRY");
        repo.Setup(r => r.Upsert(record, DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>())).ReturnsAsync(1);

        await svc.RunSuccess(record, DefaultOptions);

        record.State.Should().Be("RUNNING");
        record.RetryCount.Should().Be(0);
        record.Message.Should().Be("Ok");
        repo.Verify(r => r.Upsert(record, DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RunSuccess_WhenRunningButTimestampStale_UpdatesAndCallsUpsert()
    {
        var (svc, repo) = MakeSut();
        var record = MakeRecord(state: "RUNNING", timestamp: DateTimeOffset.UtcNow.AddMinutes(-20));
        repo.Setup(r => r.Upsert(record, DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>())).ReturnsAsync(1);

        await svc.RunSuccess(record, DefaultOptions);

        record.State.Should().Be("RUNNING");
        repo.Verify(r => r.Upsert(record, DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RunSuccess_WhenRunningAndTimestampFresh_DoesNotCallUpsert()
    {
        var (svc, repo) = MakeSut();
        var record = MakeRecord(state: "RUNNING", timestamp: DateTimeOffset.UtcNow);

        await svc.RunSuccess(record, DefaultOptions);

        repo.Verify(r => r.Upsert(It.IsAny<StatusRecord>(), It.IsAny<ChangeRequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region TryToRun

    [Fact]
    public async Task TryToRun_WhenStopped_ReturnsFalseWithoutUpsert()
    {
        var (svc, repo) = MakeSut();
        var record = MakeRecord(state: "STOPPED");

        var result = await svc.TryToRun(record, DefaultOptions);

        result.Should().BeFalse();
        repo.Verify(r => r.Upsert(It.IsAny<StatusRecord>(), It.IsAny<ChangeRequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task TryToRun_WhenRetryLimitReached_SetsStoppedCallsUpsertReturnsFalse()
    {
        var (svc, repo) = MakeSut();
        var record = MakeRecord(state: "RETRY", retryCount: 3, retryLimit: 3);
        repo.Setup(r => r.Upsert(record, DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>())).ReturnsAsync(1);

        var result = await svc.TryToRun(record, DefaultOptions);

        result.Should().BeFalse();
        record.State.Should().Be("STOPPED");
        repo.Verify(r => r.Upsert(record, DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task TryToRun_WhenRetryState_IncrementsCountCallsUpsertReturnsTrue()
    {
        var (svc, repo) = MakeSut();
        var record = MakeRecord(state: "RETRY", retryCount: 1, retryLimit: 5);
        repo.Setup(r => r.Upsert(record, DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>())).ReturnsAsync(1);

        var result = await svc.TryToRun(record, DefaultOptions);

        result.Should().BeTrue();
        record.RetryCount.Should().Be(2);
        repo.Verify(r => r.Upsert(record, DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task TryToRun_WhenRunningState_DoesNotIncrementCountCallsUpsertReturnsTrue()
    {
        var (svc, repo) = MakeSut();
        var record = MakeRecord(state: "RUNNING", retryCount: 1, retryLimit: 5);
        repo.Setup(r => r.Upsert(record, DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>())).ReturnsAsync(1);

        var result = await svc.TryToRun(record, DefaultOptions);

        result.Should().BeTrue();
        record.RetryCount.Should().Be(1);
        repo.Verify(r => r.Upsert(record, DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task TryToRun_WhenRetryCountExceedsLimit_SetsStoppedReturnsFalse()
    {
        var (svc, repo) = MakeSut();
        var record = MakeRecord(state: "RETRY", retryCount: 10, retryLimit: 3);
        repo.Setup(r => r.Upsert(record, DefaultOptions, It.IsAny<CancellationToken>(), It.IsAny<string>())).ReturnsAsync(1);

        var result = await svc.TryToRun(record, DefaultOptions);

        result.Should().BeFalse();
        record.State.Should().Be("STOPPED");
    }

    [Fact]
    public async Task TryToRun_UpdatesTimestampBeforeUpsert()
    {
        var (svc, repo) = MakeSut();
        var record = MakeRecord(state: "RUNNING", timestamp: DateTimeOffset.UtcNow.AddHours(-2));
        var before = DateTimeOffset.UtcNow;
        repo.Setup(r => r.Upsert(It.IsAny<StatusRecord>(), It.IsAny<ChangeRequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>())).ReturnsAsync(1);

        await svc.TryToRun(record, DefaultOptions);

        record.Timestamp.Should().BeOnOrAfter(before);
    }

    #endregion
}
