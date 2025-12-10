using MoleculeLookup.Core.Interfaces;
using MoleculeLookup.Core.Models;
using MoleculeLookup.Core.Patterns.ChainOfResponsibility;

namespace MoleculeLookup.Core.Validation;

/// <summary>
/// Service that validates molecule structures using the Chain of Responsibility pattern.
/// Chains together multiple validation handlers:
/// 1. ChargeBalanceHandler - validates formal charges
/// 2. BondingRulesHandler - validates valence and bond types
/// 3. StereochemistryHandler - validates chiral centers and stereo bonds
/// </summary>
public class MoleculeValidator : IMoleculeValidator
{
    private readonly IValidationHandler _handlerChain;

    /// <summary>
    /// Initializes the validator with the default chain of handlers.
    /// </summary>
    public MoleculeValidator()
    {
        // Build the chain of responsibility
        // Order matters: check charges first, then bonding, then stereochemistry
        var chargeHandler = new ChargeBalanceHandler();
        var bondingHandler = new BondingRulesHandler();
        var stereoHandler = new StereochemistryHandler();

        // Chain the handlers together
        chargeHandler
            .SetNext(bondingHandler)
            .SetNext(stereoHandler);

        _handlerChain = chargeHandler;
    }

    /// <summary>
    /// Initializes the validator with a custom handler chain.
    /// Useful for testing or customizing validation behavior.
    /// </summary>
    public MoleculeValidator(IValidationHandler handlerChain)
    {
        _handlerChain = handlerChain;
    }

    /// <summary>
    /// Validates a drawn molecule structure through all validation handlers.
    /// </summary>
    /// <param name="molecule">The drawn molecule to validate</param>
    /// <returns>ValidationResult with success/failure and any errors/warnings</returns>
    public ValidationResult Validate(DrawnMolecule molecule)
    {
        if (molecule == null)
        {
            return ValidationResult.Failure(
                Enums.ValidationFailureReason.EmptyStructure,
                "Cannot validate a null molecule");
        }

        // Pass the molecule through the chain of handlers
        return _handlerChain.Handle(molecule);
    }

    /// <summary>
    /// Creates a default validator with all standard handlers.
    /// </summary>
    public static MoleculeValidator CreateDefault()
    {
        return new MoleculeValidator();
    }

    /// <summary>
    /// Creates a validator with only the specified handlers.
    /// </summary>
    public static MoleculeValidator CreateWithHandlers(params BaseValidationHandler[] handlers)
    {
        if (handlers == null || handlers.Length == 0)
        {
            throw new ArgumentException("At least one handler must be provided");
        }

        // Chain the handlers
        for (int i = 0; i < handlers.Length - 1; i++)
        {
            handlers[i].SetNext(handlers[i + 1]);
        }

        return new MoleculeValidator(handlers[0]);
    }
}
