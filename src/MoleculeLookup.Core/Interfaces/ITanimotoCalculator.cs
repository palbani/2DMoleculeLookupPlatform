using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Interfaces;

/// <summary>
/// Interface for calculating Tanimoto coefficient between molecules.
/// </summary>
public interface ITanimotoCalculator
{
    /// <summary>
    /// Calculates the Tanimoto coefficient between two molecules.
    /// </summary>
    /// <param name="smiles1">SMILES string of the first molecule</param>
    /// <param name="smiles2">SMILES string of the second molecule</param>
    /// <returns>Tanimoto coefficient between 0 and 1</returns>
    double Calculate(string smiles1, string smiles2);

    /// <summary>
    /// Calculates the Tanimoto coefficient using pre-computed fingerprints.
    /// </summary>
    double Calculate(HashSet<int> fingerprint1, HashSet<int> fingerprint2);

    /// <summary>
    /// Generates a molecular fingerprint from a SMILES string.
    /// </summary>
    HashSet<int> GenerateFingerprint(string smiles);

    /// <summary>
    /// Finds molecules similar to the query above the threshold.
    /// </summary>
    IEnumerable<SimilarMolecule> FindSimilar(
        string querySmiles,
        IEnumerable<MoleculeMetadata> candidates,
        double threshold);
}
