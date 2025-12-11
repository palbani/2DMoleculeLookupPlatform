using MoleculeLookup.Core.Enums;
using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Patterns.ChainOfResponsibility;

/// <summary>
/// Handler 1: Validates charge balance of the molecule.
/// Checks that formal charges on atoms result in a chemically reasonable total charge.
/// Most organic molecules should have a net charge of 0, or small integer values for ions.
/// </summary>
public class ChargeBalanceHandler : BaseValidationHandler
{
    /// <summary>
    /// Maximum allowed absolute total charge for a molecule.
    /// Most drug-like molecules have charges between -2 and +2.
    /// </summary>
    private const int MaxAllowedAbsoluteCharge = 4;

    public override string HandlerName => "Charge Balance Validator";

    protected override ValidationResult Validate(DrawnMolecule molecule)
    {
        var result = new ValidationResult { IsValid = true };

        // Check for empty molecule
        if (molecule.Atoms.Count == 0)
        {
            return ValidationResult.Failure(
                ValidationFailureReason.EmptyStructure,
                "Cannot validate an empty molecule structure");
        }

        // Calculate total formal charge
        var totalCharge = molecule.TotalCharge;

        // Check if total charge is within reasonable bounds
        if (Math.Abs(totalCharge) > MaxAllowedAbsoluteCharge)
        {
            return ValidationResult.Failure(
                ValidationFailureReason.ChargeImbalance,
                $"Total molecular charge ({totalCharge}) exceeds reasonable bounds (±{MaxAllowedAbsoluteCharge}). " +
                "Please verify the formal charges on your atoms.");
        }

        // Check individual atom charges for validity
        foreach (var atom in molecule.Atoms)
        {
            if (!IsValidAtomCharge(atom))
            {
                result.AddError(
                    ValidationFailureReason.ChargeImbalance,
                    $"Atom {atom.Symbol} (ID: {atom.Id}) has an unusual formal charge of {atom.FormalCharge}",
                    atom.Id);
            }
        }

        // Add warnings for unusual but valid charges
        if (totalCharge != 0)
        {
            result.AddWarning(
                $"Molecule has a net charge of {totalCharge:+#;-#;0}. " +
                "Ensure this is intentional for ionic species.");
        }

        return result;
    }

    /// <summary>
    /// Checks if an atom's formal charge is chemically reasonable.
    /// </summary>
    private static bool IsValidAtomCharge(Atom atom)
    {
        // Define reasonable charge ranges for common elements
        var validChargeRanges = new Dictionary<string, (int min, int max)>(StringComparer.OrdinalIgnoreCase)
        {
            { "H", (-1, 1) },    // H-, H+
            { "C", (-1, 1) },    // Carbocation, carbanion
            { "N", (-1, 1) },    // Amide, ammonium
            { "O", (-1, 0) },    // Oxide, hydroxide
            { "S", (-1, 2) },    // Various sulfur species
            { "P", (-1, 1) },    // Phosphate species
            { "F", (-1, 0) },
            { "Cl", (-1, 0) },
            { "Br", (-1, 0) },
            { "I", (-1, 0) }
        };

        if (validChargeRanges.TryGetValue(atom.Symbol, out var range))
        {
            return atom.FormalCharge >= range.min && atom.FormalCharge <= range.max;
        }

        // For unknown elements, allow small charges
        return Math.Abs(atom.FormalCharge) <= 2;
    }
}
