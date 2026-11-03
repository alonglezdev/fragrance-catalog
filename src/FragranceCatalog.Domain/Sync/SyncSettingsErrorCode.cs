namespace FragranceCatalog.Domain.Sync;

/// <summary>Reasons the sync settings cannot be saved.</summary>
public enum SyncSettingsErrorCode
{
    /// <summary>The daily sync time includes seconds.</summary>
    DailySyncTimeNotWholeMinute,

    /// <summary>The time zone is not a known IANA id.</summary>
    UnknownTimeZone,

    /// <summary>The failure percentage is outside its allowed range.</summary>
    FailureThresholdPercentOutOfRange,

    /// <summary>The pause between requests is outside its allowed range.</summary>
    DelayBetweenRequestsOutOfRange,

    /// <summary>The timeout per request is outside its allowed range.</summary>
    RequestTimeoutOutOfRange,
}
