using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Interfaces;

/// <summary>
/// Interface for converting drawn molecules to SMILES strings.
/// </summary>
public interface ISmilesConverter
{
    /// <summary>
    /// Converts a drawn molecule to a canonical SMILES string.
    /// </summary>
    string ToSmiles(DrawnMolecule molecule);

    /// <summary>
    /// Parses a SMILES string back to a molecule structure.
    /// </summary>
    DrawnMolecule FromSmiles(string smiles);

    /// <summary>
    /// Validates that a SMILES string is syntactically correct.
    /// </summary>
    bool IsValidSmiles(string smiles);
}
