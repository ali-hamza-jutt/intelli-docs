namespace DocuMind.Application.Common;

/// <summary>Base for errors that map to a specific HTTP status rather than a 500.</summary>
public abstract class AppException(string message, string errorCode) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

/// <summary>400 — the request was understood but the data was unacceptable.</summary>
public sealed class ValidationAppException(string message, string errorCode = "VALIDATION_FAILED")
    : AppException(message, errorCode);

/// <summary>401 — no valid credentials.</summary>
public sealed class UnauthorizedAppException(string message, string errorCode = "UNAUTHORIZED")
    : AppException(message, errorCode);

/// <summary>404 — also used when hiding another user's resource.</summary>
public sealed class NotFoundAppException(string message, string errorCode = "NOT_FOUND")
    : AppException(message, errorCode);

/// <summary>409 — the request conflicts with existing state, e.g. a duplicate email.</summary>
public sealed class ConflictAppException(string message, string errorCode = "CONFLICT")
    : AppException(message, errorCode);
