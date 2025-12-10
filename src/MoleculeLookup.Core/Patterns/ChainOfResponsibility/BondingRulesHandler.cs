using MoleculeLookup.Core.Enums;
using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Patterns.ChainOfResponsibility;

/// <summary>
/// Handler 2: Validates bonding rules for the molecule.
/// Checks that each atom's valence (number and type of bonds) is chemically valid.
/// Ensures atoms don't exceed their maximum valence based on their element type.
/// </summary>
public class BondingRulesHandler : BaseValidationHandler
{
    public override string HandlerName => "Bonding Rules Validator";

    protected override ValidationResult Validate(DrawnMolecule molecule)
    {
        var result = new ValidationResult { IsValid = true };

        foreach (var atom in molecule.Atoms)
        {
            // Calculate current valence from bonds
            var bondValence = CalculateBondValence(molecule, atom.Id);
            var totalValence = bondValence + atom.ImplicitHydrogens;

            // Get the expected valence for this atom
            var expectedValences = GetValidValences(atom.Symbol, atom.FormalCharge);

            if (!expectedValences.Contains(totalValence))
            {
                // Check if over-valent (definite error)
                var maxValence = expectedValences.Max();
                if (totalValence > maxValence)
                {
                    result.AddError(
                        ValidationFailureReason.InvalidBondingRules,
                        $"Atom {atom.Symbol} (ID: {atom.Id}) has valence {totalValence}, " +
                        $"which exceeds maximum allowed valence of {maxValence}. " +
                        "Please check the bond orders connected to this atom.",
                        atom.Id);
                }
                else
                {
                    // Under-valent - could be intentional (radical) or missing hydrogens
                    result.AddWarning(
                        $"Atom {atom.Symbol} (ID: {atom.Id}) has valence {totalValence}. " +
                        $"Expected valences are: {string.Join(", ", expectedValences)}. " +
                        "This may indicate missing hydrogen atoms.",
                        atom.Id);
                }
            }

            // Check for invalid bond types
            var invalidBonds = ValidateBondTypes(molecule, atom);
            foreach (var error in invalidBonds)
            {
                result.AddError(ValidationFailureReason.InvalidBondingRules, error, atom.Id);
            }
        }

        // Check for disconnected fragments (optional warning)
        if (HasDisconnectedFragments(molecule))
        {
            result.AddWarning(
                "Molecule contains disconnected fragments. " +
                "If this is intentional (e.g., a salt), you can ignore this warning.");
        }

        return result;
    }

    /// <summary>
    /// Calculates the total bond valence for an atom.
    /// </summary>
    private static int CalculateBondValence(DrawnMolecule molecule, int atomId)
    {
        return molecule.GetBondsForAtom(atomId).Sum(b => b.Order);
    }

    /// <summary>
    /// Gets valid valence values for an element, accounting for formal charge.
    /// </summary>
    private static HashSet<int> GetValidValences(string symbol, int charge)
    {
        // Base valences for neutral atoms (common organic chemistry valences)
        var baseValences = symbol.ToUpper() switch
        {
            "H" => new HashSet<int> { 1 },
            "C" => new HashSet<int> { 4 },
            "N" => new HashSet<int> { 3, 5 },        // N can be trivalent or pentavalent
            "O" => new HashSet<int> { 2 },
            "S" => new HashSet<int> { 2, 4, 6 },     // S has multiple oxidation states
            "P" => new HashSet<int> { 3, 5 },
            "F" => new HashSet<int> { 1 },
            "CL" => new HashSet<int> { 1, 3, 5, 7 },
            "BR" => new HashSet<int> { 1, 3, 5 },
            "I" => new HashSet<int> { 1, 3, 5, 7 },
            "B" => new HashSet<int> { 3 },
            "SI" => new HashSet<int> { 4 },
            _ => new HashSet<int> { 4 }              // Default fallback
        };

        // Adjust valences based on formal charge
        // Positive charge typically reduces available valence
        // Negative charge typically increases available valence
        if (charge != 0)
        {
            var adjusted = new HashSet<int>();
            foreach (var v in baseValences)
            {
                adjusted.Add(v - charge);
            }
            // Also include original valences as some charged species maintain them
            foreach (var v in baseValences)
            {
                adjusted.Add(v);
            }
            return adjusted;
        }

        return baseValences;
    }

    /// <summary>
    /// Validates bond types for specific atom combinations.
    /// </summary>
    private static List<string> ValidateBondTypes(DrawnMolecule molecule, Atom atom)
    {
        var errors = new List<string>();
        var bonds = molecule.GetBondsForAtom(atom.Id).ToList();

        // Halogens should only have single bonds (except in special cases)
        var halogens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "F", "Cl", "Br", "I" };

        if (halogens.Contains(atom.Symbol))
        {
            var nonSingleBonds = bonds.Where(b => b.Type != BondType.Single).ToList();
            if (nonSingleBonds.Any() && atom.FormalCharge == 0)
            {
                errors.Add(
                    $"Halogen {atom.Symbol} (ID: {atom.Id}) has non-single bonds, " +
                    "which is unusual for neutral halogens.");
            }
        }

        // Check for impossible triple bonds
        if (atom.Symbol.Equals("O", StringComparison.OrdinalIgnoreCase))
        {
            var tripleBonds = bonds.Where(b => b.Type == BondType.Triple).ToList();
            if (tripleBonds.Any())
            {
                errors.Add(
                    $"Oxygen atom (ID: {atom.Id}) cannot form triple bonds.");
            }
        }

        return errors;
    }

    /// <summary>
    /// Checks if the molecule has disconnected fragments using BFS.
    /// </summary>
    private static bool HasDisconnectedFragments(DrawnMolecule molecule)
    {
        if (molecule.Atoms.Count <= 1)
            return false;

        var visited = new HashSet<int>();
        var queue = new Queue<int>();

        // Start BFS from the first atom
        queue.Enqueue(molecule.Atoms[0].Id);
        visited.Add(molecule.Atoms[0].Id);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            foreach (var neighbor in molecule.GetConnectedAtoms(currentId))
            {
                if (!visited.Contains(neighbor.Id))
                {
                    visited.Add(neighbor.Id);
                    queue.Enqueue(neighbor.Id);
                }
            }
        }

        // If we haven't visited all atoms, there are disconnected fragments
        return visited.Count != molecule.Atoms.Count;
    }
}
