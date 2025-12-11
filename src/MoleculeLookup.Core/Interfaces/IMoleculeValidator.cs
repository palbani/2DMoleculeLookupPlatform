using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Interfaces;

/// <summary>
/// Interface for the Chain of Responsibility validation handlers.
/// </summary>
public interface IValidationHandler
{
    /// <summary>
    /// Sets the next handler in the chain.
    /// </summary>
    IValidationHandler SetNext(IValidationHandler handler);

    /// <summary>
    /// Handles the validation request, passing to next handler if successful.
    /// </summary>
    ValidationResult Handle(DrawnMolecule molecule);
}

/// <summary>
/// Interface for the complete molecule validation service.
/// </summary>
public interface IMoleculeValidator
{
    /// <summary>
    /// Validates a drawn molecule structure through all validation handlers.
    /// </summary>
    ValidationResult Validate(DrawnMolecule molecule);
}
