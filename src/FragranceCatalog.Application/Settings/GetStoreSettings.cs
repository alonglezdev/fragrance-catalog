namespace FragranceCatalog.Application.Settings;

/// <summary>Shows the administrator the store settings.</summary>
/// <param name="store">Settings data.</param>
public sealed class GetStoreSettings(ISettingsStore store)
{
    /// <summary>Reads the settings.</summary>
    /// <param name="cancellationToken">Stops the read.</param>
    public async Task<StoreSettingsDto> ExecuteAsync(CancellationToken cancellationToken) =>
        StoreSettingsDto.From(await store.GetStoreSettingsAsync(cancellationToken));
}
