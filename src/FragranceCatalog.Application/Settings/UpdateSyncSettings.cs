using FragranceCatalog.Application.Common;
using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Application.Settings;

/// <summary>Saves the sync settings and reschedules the daily job. A sync already running finishes with the values it started with.</summary>
/// <param name="store">Settings data.</param>
/// <param name="scheduler">Daily job.</param>
public sealed class UpdateSyncSettings(ISettingsStore store, ISyncScheduler scheduler)
{
    /// <summary>Validates and saves the settings; invalid values change nothing, are all reported together and leave the job as it was.</summary>
    /// <param name="values">Proposed settings.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    public async Task<Result<SyncSettingsDto>> ExecuteAsync(SyncSettingsValues values, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(values);

        var errors = SyncSettings.Validate(values.DailySyncTime, values.TimeZoneId, values.FailureThresholdPercent, values.DelayBetweenRequestsMs, values.RequestTimeoutSeconds);
        if (errors.Count > 0)
        {
            return Result.Failure<SyncSettingsDto>(errors.Select(error => Error.Validation(error.Code, error.Value)));
        }

        var settings = await store.GetSyncSettingsAsync(cancellationToken);
        settings.Update(values.IsDailySyncEnabled, values.DailySyncTime, values.TimeZoneId, values.FailureThresholdPercent, values.DelayBetweenRequestsMs, values.RequestTimeoutSeconds);
        await store.SaveChangesAsync(cancellationToken);
        await scheduler.ApplyScheduleAsync(settings, cancellationToken);
        return Result.Success(SyncSettingsDto.From(settings));
    }
}
