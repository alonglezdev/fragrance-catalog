namespace FragranceCatalog.Domain.Sync;

/// <summary>A problem found in the sync settings.</summary>
/// <param name="Code">What is wrong.</param>
/// <param name="Value">The value that caused the problem, as text.</param>
public sealed record SyncSettingsError(SyncSettingsErrorCode Code, string Value);
