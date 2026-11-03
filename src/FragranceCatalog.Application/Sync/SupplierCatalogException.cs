namespace FragranceCatalog.Application.Sync;

/// <summary>
/// A supplier page could not be downloaded, or its content is not what the reader expects, for example a missing price
/// or prices that contradict each other. The sync records it as an error for that URL and continues.
/// </summary>
public sealed class SupplierCatalogException : Exception
{
    /// <summary>Creates the exception with a message that explains what is wrong with the page.</summary>
    /// <param name="message">What is missing or inconsistent.</param>
    public SupplierCatalogException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception wrapping the error that made the page unreadable.</summary>
    /// <param name="message">What is missing or inconsistent.</param>
    /// <param name="innerException">The original error.</param>
    public SupplierCatalogException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
