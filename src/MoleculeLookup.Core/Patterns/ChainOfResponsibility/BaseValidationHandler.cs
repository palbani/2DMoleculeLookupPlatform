using MoleculeLookup.Core.Interfaces;
using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Patterns.ChainOfResponsibility;

/// <summary>
/// Abstract base class for the Chain of Responsibility pattern.
/// Each handler validates one aspect of the molecule structure.
/// If validation passes, the request is forwarded to the next handler.
/// If validation fails, the chain stops and returns the error.
/// </summary>
public abstract class BaseValidationHandler : IValidationHandler
{
    private IValidationHandler? _nextHandler;

    /// <summary>
    /// Sets the next handler in the chain.
    /// Returns the next handler to allow fluent chaining.
    /// </summary>
    public IValidationHandler SetNext(IValidationHandler handler)
    {
        _nextHandler = handler;
        return handler;
    }

    /// <summary>
    /// Handles the validation request.
    /// Validates using the concrete implementation, then passes to next handler if valid.
    /// </summary>
    public virtual ValidationResult Handle(DrawnMolecule molecule)
    {
        // Perform this handler's validation
        var result = Validate(molecule);

        // If validation failed, stop the chain and return the error
        if (!result.IsValid)
        {
            return result;
        }

        // If there's a next handler, pass the molecule to it
        if (_nextHandler != null)
        {
            return _nextHandler.Handle(molecule);
        }

        // All validations passed
        return ValidationResult.Success();
    }

    /// <summary>
    /// Abstract method for concrete handlers to implement their specific validation logic.
    /// </summary>
    protected abstract ValidationResult Validate(DrawnMolecule molecule);

    /// <summary>
    /// Gets the name of this validation handler for logging/error messages.
    /// </summary>
    public abstract string HandlerName { get; }
}
