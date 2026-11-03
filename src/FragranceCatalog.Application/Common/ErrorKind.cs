namespace FragranceCatalog.Application.Common;

/// <summary>Family of an expected error, which decides how the API reports it.</summary>
public enum ErrorKind
{
    /// <summary>The submitted values are not acceptable.</summary>
    Validation,

    /// <summary>The referenced record does not exist.</summary>
    NotFound,

    /// <summary>The request is valid but clashes with the current state.</summary>
    Conflict,
}
