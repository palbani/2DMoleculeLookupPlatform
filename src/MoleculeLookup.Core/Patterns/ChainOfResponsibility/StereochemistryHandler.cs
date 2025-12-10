using MoleculeLookup.Core.Enums;
using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Patterns.ChainOfResponsibility;

/// <summary>
/// Handler 3: Validates stereochemistry of the molecule.
/// Checks for presence and correctness of chiral centers and geometric isomers.
/// Validates that stereo assignments (R/S, E/Z) are chemically valid.
/// </summary>
public class StereochemistryHandler : BaseValidationHandler
{
    public override string HandlerName => "Stereochemistry Validator";

    protected override ValidationResult Validate(DrawnMolecule molecule)
    {
        var result = new ValidationResult { IsValid = true };

        // Find potential chiral centers
        var potentialChiralCenters = FindPotentialChiralCenters(molecule);

        // Find potential E/Z isomer centers (double bonds)
        var potentialEZCenters = FindPotentialEZCenters(molecule);

        // Validate marked chiral centers
        foreach (var atom in molecule.Atoms.Where(a => a.IsChiralCenter))
        {
            if (!potentialChiralCenters.Contains(atom.Id))
            {
                result.AddError(
                    ValidationFailureReason.InvalidStereochemistry,
                    $"Atom {atom.Symbol} (ID: {atom.Id}) is marked as a chiral center " +
                    "but does not have four different substituents. " +
                    "A chiral center requires a tetrahedral carbon with four distinct groups.",
                    atom.Id);
            }

            // Validate chirality configuration
            if (!string.IsNullOrEmpty(atom.ChiralConfiguration))
            {
                if (atom.ChiralConfiguration != "R" && atom.ChiralConfiguration != "S")
                {
                    result.AddError(
                        ValidationFailureReason.InvalidStereochemistry,
                        $"Atom {atom.Symbol} (ID: {atom.Id}) has invalid chiral configuration " +
                        $"'{atom.ChiralConfiguration}'. Must be 'R' or 'S'.",
                        atom.Id);
                }
            }
        }

        // Warn about unmarked potential chiral centers
        var unmarkedChiralCenters = potentialChiralCenters
            .Where(id => !molecule.Atoms.First(a => a.Id == id).IsChiralCenter)
            .ToList();

        if (unmarkedChiralCenters.Any())
        {
            var atomIds = string.Join(", ", unmarkedChiralCenters);
            result.AddWarning(
                $"Potential chiral center(s) detected at atom ID(s): {atomIds}. " +
                "Consider specifying R/S configuration for stereochemically accurate searches.");
        }

        // Warn about E/Z isomerism in double bonds
        if (potentialEZCenters.Any())
        {
            result.AddWarning(
                $"Molecule contains {potentialEZCenters.Count} double bond(s) that may have E/Z isomerism. " +
                "Ensure correct geometric configuration is specified if stereochemistry is important.");
        }

        // Validate stereo bonds (wedge/dash) are attached to chiral centers
        ValidateStereoBonds(molecule, result);

        return result;
    }

    /// <summary>
    /// Finds atoms that could be chiral centers (sp3 carbon with 4 different substituents).
    /// </summary>
    private HashSet<int> FindPotentialChiralCenters(DrawnMolecule molecule)
    {
        var chiralCenters = new HashSet<int>();

        foreach (var atom in molecule.Atoms)
        {
            // Only carbon atoms are commonly chiral centers in organic chemistry
            // (though N, P, S can also be chiral in some cases)
            if (!atom.Symbol.Equals("C", StringComparison.OrdinalIgnoreCase) &&
                !atom.Symbol.Equals("N", StringComparison.OrdinalIgnoreCase) &&
                !atom.Symbol.Equals("S", StringComparison.OrdinalIgnoreCase) &&
                !atom.Symbol.Equals("P", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Get all bonds for this atom
            var bonds = molecule.GetBondsForAtom(atom.Id).ToList();

            // Skip if not tetrahedral (needs 4 connections for chirality)
            // Account for implicit hydrogens
            var totalConnections = bonds.Count + atom.ImplicitHydrogens;
            if (totalConnections != 4)
                continue;

            // Skip if any double or triple bonds (not sp3)
            if (bonds.Any(b => b.Type == BondType.Double || b.Type == BondType.Triple))
                continue;

            // Check if all four substituents are different
            if (HasFourDifferentSubstituents(molecule, atom, bonds))
            {
                chiralCenters.Add(atom.Id);
            }
        }

        return chiralCenters;
    }

    /// <summary>
    /// Checks if an atom has four different substituents (required for chirality).
    /// Uses a simplified approach based on immediate neighbors.
    /// </summary>
    private bool HasFourDifferentSubstituents(DrawnMolecule molecule, Atom centerAtom, List<Bond> bonds)
    {
        var substituents = new List<string>();

        // Add connected atoms
        foreach (var bond in bonds)
        {
            var neighborId = bond.Atom1Id == centerAtom.Id ? bond.Atom2Id : bond.Atom1Id;
            var neighbor = molecule.Atoms.First(a => a.Id == neighborId);

            // Create a simple substituent signature (could be made more sophisticated)
            var signature = GetSubstituentSignature(molecule, neighbor, centerAtom.Id, 2);
            substituents.Add(signature);
        }

        // Add implicit hydrogens
        for (int i = 0; i < centerAtom.ImplicitHydrogens; i++)
        {
            substituents.Add("H");
        }

        // Check if all substituents are unique
        return substituents.Distinct().Count() == substituents.Count;
    }

    /// <summary>
    /// Creates a simple signature for a substituent for comparison.
    /// Uses depth-limited traversal to distinguish substituents.
    /// </summary>
    private string GetSubstituentSignature(DrawnMolecule molecule, Atom startAtom, int excludeAtomId, int depth)
    {
        if (depth == 0)
            return startAtom.Symbol;

        var neighbors = molecule.GetConnectedAtoms(startAtom.Id)
            .Where(a => a.Id != excludeAtomId)
            .OrderBy(a => a.Symbol)
            .ThenBy(a => molecule.GetBondsForAtom(a.Id).Count());

        var neighborSignatures = neighbors
            .Select(n => GetSubstituentSignature(molecule, n, startAtom.Id, depth - 1));

        return $"{startAtom.Symbol}({string.Join(",", neighborSignatures)})";
    }

    /// <summary>
    /// Finds double bonds that could have E/Z isomerism.
    /// </summary>
    private List<int> FindPotentialEZCenters(DrawnMolecule molecule)
    {
        var ezBonds = new List<int>();

        foreach (var bond in molecule.Bonds.Where(b => b.Type == BondType.Double))
        {
            var atom1 = molecule.Atoms.First(a => a.Id == bond.Atom1Id);
            var atom2 = molecule.Atoms.First(a => a.Id == bond.Atom2Id);

            // Both atoms need at least one other substituent besides the double bond partner
            var atom1Bonds = molecule.GetBondsForAtom(atom1.Id).Count();
            var atom2Bonds = molecule.GetBondsForAtom(atom2.Id).Count();

            // Need 2+ bonds on each atom for E/Z possibility (one is the double bond)
            if (atom1Bonds >= 2 && atom2Bonds >= 2)
            {
                // Check that substituents on each end are different
                var atom1Neighbors = molecule.GetConnectedAtoms(atom1.Id)
                    .Where(a => a.Id != atom2.Id)
                    .Select(a => a.Symbol)
                    .ToList();

                var atom2Neighbors = molecule.GetConnectedAtoms(atom2.Id)
                    .Where(a => a.Id != atom1.Id)
                    .Select(a => a.Symbol)
                    .ToList();

                // If each end has different substituents, E/Z is possible
                if (atom1Neighbors.Distinct().Count() == atom1Neighbors.Count ||
                    atom2Neighbors.Distinct().Count() == atom2Neighbors.Count)
                {
                    ezBonds.Add(bond.Id);
                }
            }
        }

        return ezBonds;
    }

    /// <summary>
    /// Validates that stereo bonds (wedge/dash) are properly attached to chiral centers.
    /// </summary>
    private void ValidateStereoBonds(DrawnMolecule molecule, ValidationResult result)
    {
        var stereoBonds = molecule.Bonds.Where(b =>
            b.Stereo == BondStereo.Up || b.Stereo == BondStereo.Down);

        foreach (var bond in stereoBonds)
        {
            var atom1 = molecule.Atoms.First(a => a.Id == bond.Atom1Id);
            var atom2 = molecule.Atoms.First(a => a.Id == bond.Atom2Id);

            // At least one end should be a chiral center
            if (!atom1.IsChiralCenter && !atom2.IsChiralCenter)
            {
                result.AddWarning(
                    $"Stereo bond (ID: {bond.Id}) is not connected to a designated chiral center. " +
                    "This may result in undefined stereochemistry.",
                    bond.Atom1Id);
            }
        }
    }
}
