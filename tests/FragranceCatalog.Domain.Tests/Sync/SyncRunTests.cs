using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Domain.Tests.Sync;

public sealed class SyncRunTests
{
    private static readonly DateTimeOffset _startedAt = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_ManualTrigger_IsRunning()
    {
        var run = SyncRun.Start(SyncTrigger.Manual, _startedAt);

        run.Status.ShouldBe(SyncStatus.Running);
        run.Trigger.ShouldBe(SyncTrigger.Manual);
        run.FinishedAt.ShouldBeNull();
        run.IsFinished.ShouldBeFalse();
    }

    [Fact]
    public void RecordError_TwoFailedPages_CountsBoth()
    {
        var run = SyncRun.Start(SyncTrigger.Scheduled, _startedAt);

        run.RecordError("https://magnaperfumes.com/a", "Timeout");
        run.RecordError("https://magnaperfumes.com/b", "Missing price");

        run.ErrorCount.ShouldBe(2);
        run.Errors.Select(error => error.Url).ShouldBe(["https://magnaperfumes.com/a", "https://magnaperfumes.com/b"]);
    }

    [Fact]
    public void Complete_RunningRun_StoresCountsAndSucceeds()
    {
        var run = SyncRun.Start(SyncTrigger.Scheduled, _startedAt);

        run.Complete(new SyncChangeCounts(Created: 3, PriceChanged: 5, Discontinued: 1, Reactivated: 2), _startedAt.AddMinutes(10));

        run.Status.ShouldBe(SyncStatus.Succeeded);
        run.CreatedCount.ShouldBe(3);
        run.PriceChangedCount.ShouldBe(5);
        run.DiscontinuedCount.ShouldBe(1);
        run.ReactivatedCount.ShouldBe(2);
        run.FinishedAt.ShouldBe(_startedAt.AddMinutes(10));
    }

    [Fact]
    public void Fail_RunWithErrors_KeepsErrorsAndZeroCounts()
    {
        var run = SyncRun.Start(SyncTrigger.Scheduled, _startedAt);
        run.RecordError("https://magnaperfumes.com/a", "Timeout");

        run.Fail(_startedAt.AddMinutes(10));

        run.Status.ShouldBe(SyncStatus.Failed);
        run.ErrorCount.ShouldBe(1);
        run.CreatedCount.ShouldBe(0);
    }

    [Fact]
    public void Complete_AlreadyFailedRun_ThrowsInvalidOperationException()
    {
        var run = SyncRun.Start(SyncTrigger.Scheduled, _startedAt);
        run.Fail(_startedAt.AddMinutes(1));

        Should.Throw<InvalidOperationException>(() => run.Complete(new SyncChangeCounts(0, 0, 0, 0), _startedAt.AddMinutes(2)));
    }

    [Fact]
    public void RecordError_FinishedRun_ThrowsInvalidOperationException()
    {
        var run = SyncRun.Start(SyncTrigger.Scheduled, _startedAt);
        run.Fail(_startedAt.AddMinutes(1));

        Should.Throw<InvalidOperationException>(() => run.RecordError("https://magnaperfumes.com/a", "Timeout"));
    }

    [Fact]
    public void Fail_FinishedBeforeStart_ThrowsArgumentOutOfRangeException()
    {
        var run = SyncRun.Start(SyncTrigger.Scheduled, _startedAt);

        Should.Throw<ArgumentOutOfRangeException>(() => run.Fail(_startedAt.AddSeconds(-1)));
    }

    [Fact]
    public void Complete_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        var run = SyncRun.Start(SyncTrigger.Scheduled, _startedAt);

        Should.Throw<ArgumentOutOfRangeException>(() => run.Complete(new SyncChangeCounts(-1, 0, 0, 0), _startedAt.AddMinutes(1)));
    }
}
