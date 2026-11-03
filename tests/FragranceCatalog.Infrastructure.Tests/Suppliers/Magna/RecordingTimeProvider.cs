namespace FragranceCatalog.Infrastructure.Tests.Suppliers.Magna;

internal sealed class RecordingTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    public List<TimeSpan> RequestedDelays { get; } = [];

    public override DateTimeOffset GetUtcNow() => _now;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        RequestedDelays.Add(dueTime);
        callback(state);
        return new CompletedTimer();
    }

    private sealed class CompletedTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
