namespace FragranceCatalog.Domain.Store;

/// <summary>A problem found in the store settings.</summary>
/// <param name="Code">What is wrong.</param>
/// <param name="Value">The value that caused the problem, or the unknown placeholder.</param>
public sealed record StoreSettingsError(StoreSettingsErrorCode Code, string Value);
