namespace FragranceCatalog.Application.Settings;

/// <summary>Sync settings proposed by the administrator.</summary>
/// <param name="IsDailySyncEnabled">Whether the daily sync is scheduled.</param>
/// <param name="DailySyncTime">Local time of the daily sync, in whole minutes.</param>
/// <param name="TimeZoneId">IANA time zone of that time.</param>
/// <param name="FailureThresholdPercent">Failure percentage that cancels a sync.</param>
/// <param name="DelayBetweenRequestsMs">Pause between supplier requests in milliseconds.</param>
/// <param name="RequestTimeoutSeconds">Timeout per supplier page in seconds.</param>
public sealed record SyncSettingsValues(
    bool IsDailySyncEnabled,
    TimeOnly DailySyncTime,
    string TimeZoneId,
    int FailureThresholdPercent,
    int DelayBetweenRequestsMs,
    int RequestTimeoutSeconds);
