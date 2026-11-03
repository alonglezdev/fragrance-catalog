using FragranceCatalog.Domain.Common;

namespace FragranceCatalog.Domain.Sync;

/// <summary>
/// Operational settings of the catalog sync, edited from the admin panel. There is exactly one row.
/// A running sync keeps the values it started with.
/// </summary>
public sealed class SyncSettings
{
    /// <summary>Identifier of the single settings row.</summary>
    public const int SingletonId = 1;

    /// <summary>Lowest failure percentage that may cancel a sync.</summary>
    public const int MinFailureThresholdPercent = 1;

    /// <summary>Highest failure percentage that may cancel a sync.</summary>
    public const int MaxFailureThresholdPercent = 100;

    /// <summary>Shortest pause between supplier requests, to stay polite with the supplier site.</summary>
    public const int MinDelayBetweenRequestsMs = 250;

    /// <summary>Longest pause between supplier requests.</summary>
    public const int MaxDelayBetweenRequestsMs = 10_000;

    /// <summary>Shortest time to wait for one supplier page.</summary>
    public const int MinRequestTimeoutSeconds = 5;

    /// <summary>Longest time to wait for one supplier page.</summary>
    public const int MaxRequestTimeoutSeconds = 120;

    /// <summary>Time zone used until the administrator changes it.</summary>
    public const string DefaultTimeZoneId = "America/Mexico_City";

    /// <summary>Failure percentage that cancels a sync, until the administrator changes it.</summary>
    public const int DefaultFailureThresholdPercent = 20;

    /// <summary>Pause between supplier requests, until the administrator changes it.</summary>
    public const int DefaultDelayBetweenRequestsMs = 1_000;

    /// <summary>Timeout per supplier page, until the administrator changes it.</summary>
    public const int DefaultRequestTimeoutSeconds = 30;

    /// <summary>Time of day the daily sync runs, until the administrator changes it.</summary>
    public static readonly TimeOnly DefaultDailySyncTime = new(4, 0);

    private SyncSettings(int id, string timeZoneId)
    {
        Id = id;
        TimeZoneId = timeZoneId;
    }

    /// <summary>Always <see cref="SingletonId"/>.</summary>
    public int Id { get; private set; }

    /// <summary>Whether the daily sync is scheduled; manual syncs work either way.</summary>
    public bool IsDailySyncEnabled { get; private set; }

    /// <summary>Local time of day, in <see cref="TimeZoneId"/>, when the daily sync runs. Always a whole minute.</summary>
    public TimeOnly DailySyncTime { get; private set; }

    /// <summary>IANA time zone the daily sync time is expressed in.</summary>
    public string TimeZoneId { get; private set; }

    /// <summary>When more than this percentage of products fail, the sync is cancelled and nothing is changed.</summary>
    public int FailureThresholdPercent { get; private set; }

    /// <summary>Pause between supplier requests in milliseconds.</summary>
    public int DelayBetweenRequestsMs { get; private set; }

    /// <summary>Time to wait for one supplier page before counting it as failed.</summary>
    public int RequestTimeoutSeconds { get; private set; }

    /// <summary>Settings for a new store: daily sync on at 04:00 Mexico City time, 20% failure threshold, 1 s pause and 30 s timeout.</summary>
    public static SyncSettings CreateDefault()
    {
        var settings = new SyncSettings(SingletonId, DefaultTimeZoneId);
        settings.Update(
            isDailySyncEnabled: true,
            DefaultDailySyncTime,
            DefaultTimeZoneId,
            DefaultFailureThresholdPercent,
            DefaultDelayBetweenRequestsMs,
            DefaultRequestTimeoutSeconds);
        return settings;
    }

    /// <summary>Whether the text is an IANA time zone known to the system, such as "America/Mexico_City".</summary>
    /// <param name="timeZoneId">Candidate time zone id.</param>
    public static bool IsKnownIanaTimeZone(string timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId)
        && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone)
        && timeZone.HasIanaId;

    /// <summary>Replaces every sync setting at once, rejecting values outside the allowed ranges.</summary>
    /// <param name="isDailySyncEnabled">Whether the daily sync is scheduled.</param>
    /// <param name="dailySyncTime">Local time of the daily sync, in whole minutes.</param>
    /// <param name="timeZoneId">IANA time zone of the daily sync time.</param>
    /// <param name="failureThresholdPercent">Failure percentage that cancels a sync.</param>
    /// <param name="delayBetweenRequestsMs">Pause between supplier requests in milliseconds.</param>
    /// <param name="requestTimeoutSeconds">Timeout per supplier page in seconds.</param>
    public void Update(
        bool isDailySyncEnabled,
        TimeOnly dailySyncTime,
        string timeZoneId,
        int failureThresholdPercent,
        int delayBetweenRequestsMs,
        int requestTimeoutSeconds)
    {
        if (dailySyncTime.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            throw new ArgumentException("The daily sync time must be a whole minute.", nameof(dailySyncTime));
        }

        if (!IsKnownIanaTimeZone(timeZoneId))
        {
            throw new ArgumentException($"'{timeZoneId}' is not a known IANA time zone.", nameof(timeZoneId));
        }

        Guard.ThrowIfOutOfRange(failureThresholdPercent, MinFailureThresholdPercent, MaxFailureThresholdPercent);
        Guard.ThrowIfOutOfRange(delayBetweenRequestsMs, MinDelayBetweenRequestsMs, MaxDelayBetweenRequestsMs);
        Guard.ThrowIfOutOfRange(requestTimeoutSeconds, MinRequestTimeoutSeconds, MaxRequestTimeoutSeconds);

        IsDailySyncEnabled = isDailySyncEnabled;
        DailySyncTime = dailySyncTime;
        TimeZoneId = timeZoneId;
        FailureThresholdPercent = failureThresholdPercent;
        DelayBetweenRequestsMs = delayBetweenRequestsMs;
        RequestTimeoutSeconds = requestTimeoutSeconds;
    }
}
