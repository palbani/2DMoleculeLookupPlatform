using MoleculeLookup.Core.Enums;

namespace MoleculeLookup.Core.Models;

/// <summary>
/// Result of molecule structure validation through the Chain of Responsibility.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public ValidationFailureReason FailureReason { get; set; } = ValidationFailureReason.None;
    public string? ErrorMessage { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates a failed validation result with the specified reason.
    /// </summary>
    public static ValidationResult Failure(ValidationFailureReason reason, string message)
    {
        return new ValidationResult
        {
            IsValid = false,
            FailureReason = reason,
            ErrorMessage = message,
            Errors = new List<ValidationError>
            {
                new ValidationError { Reason = reason, Message = message }
            }
        };
    }

    /// <summary>
    /// Adds an error to the validation result.
    /// </summary>
    public void AddError(ValidationFailureReason reason, string message, int? atomId = null)
    {
        IsValid = false;
        FailureReason = reason;
        ErrorMessage ??= message;
        Errors.Add(new ValidationError
        {
            Reason = reason,
            Message = message,
            AtomId = atomId
        });
    }

    /// <summary>
    /// Adds a warning (non-fatal) to the validation result.
    /// </summary>
    public void AddWarning(string message, int? atomId = null)
    {
        Warnings.Add(new ValidationWarning
        {
            Message = message,
            AtomId = atomId
        });
    }
}

/// <summary>
/// Represents a validation error that prevents molecule processing.
/// </summary>
public class ValidationError
{
    public ValidationFailureReason Reason { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? AtomId { get; set; }
}

/// <summary>
/// Represents a validation warning that does not prevent processing.
/// </summary>
public class ValidationWarning
{
    public string Message { get; set; } = string.Empty;
    public int? AtomId { get; set; }
}
