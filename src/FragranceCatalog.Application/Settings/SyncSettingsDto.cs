using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Application.Settings;

/// <summary>Sync settings as the admin panel shows them.</summary>
/// <param name="IsDailySyncEnabled">Whether the daily sync is scheduled.</param>
/// <param name="DailySyncTime">Local time of the daily sync.</param>
/// <param name="TimeZoneId">IANA time zone of that time.</param>
/// <param name="FailureThresholdPercent">Failure percentage that cancels a sync.</param>
/// <param name="DelayBetweenRequestsMs">Pause between supplier requests in milliseconds.</param>
/// <param name="RequestTimeoutSeconds">Timeout per supplier page in seconds.</param>
public sealed record SyncSettingsDto(
    bool IsDailySyncEnabled,
    TimeOnly DailySyncTime,
    string TimeZoneId,
    int FailureThresholdPercent,
    int DelayBetweenRequestsMs,
    int RequestTimeoutSeconds)
{
    /// <summary>Copies the settings entity.</summary>
    /// <param name="settings">The settings.</param>
    public static SyncSettingsDto From(SyncSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new SyncSettingsDto(
            settings.IsDailySyncEnabled,
            settings.DailySyncTime,
            settings.TimeZoneId,
            settings.FailureThresholdPercent,
            settings.DelayBetweenRequestsMs,
            settings.RequestTimeoutSeconds);
    }
}
