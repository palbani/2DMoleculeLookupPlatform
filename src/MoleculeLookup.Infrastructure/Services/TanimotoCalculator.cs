using System;
using System.Collections.Generic;
using System.Linq;
using MoleculeLookup.Core.Interfaces;
using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Infrastructure.Services;

/// <summary>
/// Calculates Tanimoto coefficient (Jaccard similarity) between molecules.
///
/// The Tanimoto coefficient is defined as:
/// T(A,B) = |A ∩ B| / |A ∪ B| = |A ∩ B| / (|A| + |B| - |A ∩ B|)
///
/// Where A and B are molecular fingerprint bit sets.
/// Result ranges from 0 (completely different) to 1 (identical).
///
/// This implementation uses Morgan/Circular fingerprints which encode
/// the molecular structure as a set of hashed substructure features.
/// </summary>
public class TanimotoCalculator : ITanimotoCalculator
{
    // Morgan fingerprint parameters
    private const int FingerprintRadius = 2;      // Radius for circular fingerprints
    private const int FingerprintSize = 2048;     // Number of bits in fingerprint

    /// <summary>
    /// Calculates the Tanimoto coefficient between two molecules given their SMILES strings.
    /// </summary>
    public double Calculate(string smiles1, string smiles2)
    {
        if (string.IsNullOrWhiteSpace(smiles1) || string.IsNullOrWhiteSpace(smiles2))
            return 0.0;

        var fingerprint1 = GenerateFingerprint(smiles1);
        var fingerprint2 = GenerateFingerprint(smiles2);

        return Calculate(fingerprint1, fingerprint2);
    }

    /// <summary>
    /// Calculates the Tanimoto coefficient using pre-computed fingerprints.
    /// This is more efficient when comparing one molecule against many.
    /// </summary>
    public double Calculate(HashSet<int> fingerprint1, HashSet<int> fingerprint2)
    {
        if (fingerprint1.Count == 0 && fingerprint2.Count == 0)
            return 1.0; // Both empty = identical

        if (fingerprint1.Count == 0 || fingerprint2.Count == 0)
            return 0.0; // One empty = no similarity

        // Calculate intersection
        var intersection = fingerprint1.Intersect(fingerprint2).Count();

        // Tanimoto formula: intersection / (A + B - intersection)
        var union = fingerprint1.Count + fingerprint2.Count - intersection;

        return union > 0 ? (double)intersection / union : 0.0;
    }

    /// <summary>
    /// Generates a molecular fingerprint from a SMILES string.
    /// Uses a simplified Morgan/circular fingerprint approach.
    /// </summary>
    public HashSet<int> GenerateFingerprint(string smiles)
    {
        var fingerprint = new HashSet<int>();

        if (string.IsNullOrWhiteSpace(smiles))
            return fingerprint;

        // Parse atoms and generate features
        var atoms = ParseAtomsFromSmiles(smiles);
        var bonds = ParseBondsFromSmiles(smiles);

        // Generate atom-centered circular features at different radii
        for (int radius = 0; radius <= FingerprintRadius; radius++)
        {
            for (int atomIndex = 0; atomIndex < atoms.Count; atomIndex++)
            {
                var feature = GenerateCircularFeature(atoms, bonds, atomIndex, radius);
                var bit = Math.Abs(feature.GetHashCode()) % FingerprintSize;
                fingerprint.Add(bit);
            }
        }

        // Add additional features based on functional groups
        AddFunctionalGroupFeatures(smiles, fingerprint);

        return fingerprint;
    }

    /// <summary>
    /// Finds molecules similar to the query above the specified threshold.
    /// </summary>
    public IEnumerable<SimilarMolecule> FindSimilar(
        string querySmiles,
        IEnumerable<MoleculeMetadata> candidates,
        double threshold)
    {
        var queryFingerprint = GenerateFingerprint(querySmiles);
        var results = new List<SimilarMolecule>();

        foreach (var candidate in candidates)
        {
            // Use pre-computed fingerprint if available
            HashSet<int> candidateFingerprint;

            if (!string.IsNullOrEmpty(candidate.FingerprintBits))
            {
                candidateFingerprint = ParseFingerprintBits(candidate.FingerprintBits);
            }
            else
            {
                candidateFingerprint = GenerateFingerprint(candidate.SmilesString);
            }

            var similarity = Calculate(queryFingerprint, candidateFingerprint);

            if (similarity >= threshold)
            {
                results.Add(new SimilarMolecule
                {
                    Metadata = candidate,
                    TanimotoCoefficient = similarity
                });
            }
        }

        return results.OrderByDescending(r => r.TanimotoCoefficient);
    }

    /// <summary>
    /// Serializes a fingerprint to a string for storage.
    /// </summary>
    public static string SerializeFingerprint(HashSet<int> fingerprint)
    {
        return string.Join(",", fingerprint.OrderBy(b => b));
    }

    /// <summary>
    /// Parses a fingerprint from a serialized string.
    /// </summary>
    public static HashSet<int> ParseFingerprintBits(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
            return new HashSet<int>();

        return serialized
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToHashSet();
    }

    #region Private Helper Methods

    /// <summary>
    /// Parses atoms from a SMILES string (simplified parser).
    /// </summary>
    private List<string> ParseAtomsFromSmiles(string smiles)
    {
        var atoms = new List<string>();
        var i = 0;

        while (i < smiles.Length)
        {
            var c = smiles[i];

            // Skip bonds and brackets
            if (c == '(' || c == ')' || c == '-' || c == '=' || c == '#' || c == ':')
            {
                i++;
                continue;
            }

            // Skip ring closures
            if (char.IsDigit(c))
            {
                i++;
                continue;
            }

            // Handle bracketed atoms
            if (c == '[')
            {
                var endBracket = smiles.IndexOf(']', i);
                if (endBracket > i)
                {
                    var atomStr = smiles.Substring(i + 1, endBracket - i - 1);
                    atoms.Add(ExtractElementFromBracket(atomStr));
                    i = endBracket + 1;
                    continue;
                }
            }

            // Handle regular atoms
            if (char.IsLetter(c))
            {
                var symbol = c.ToString();
                if (i + 1 < smiles.Length && char.IsLower(smiles[i + 1]))
                {
                    symbol += smiles[i + 1];
                    i++;
                }
                atoms.Add(symbol);
            }

            i++;
        }

        return atoms;
    }

    /// <summary>
    /// Parses bonds from a SMILES string (simplified parser).
    /// Returns adjacency information.
    /// </summary>
    private Dictionary<int, List<(int neighbor, int bondOrder)>> ParseBondsFromSmiles(string smiles)
    {
        var bonds = new Dictionary<int, List<(int, int)>>();
        var atomIndex = -1;
        var prevAtomIndex = -1;
        var bondOrder = 1;
        var branchStack = new Stack<int>();
        var ringAtoms = new Dictionary<int, int>();

        for (int i = 0; i < smiles.Length; i++)
        {
            var c = smiles[i];

            if (c == '(')
            {
                branchStack.Push(atomIndex);
            }
            else if (c == ')')
            {
                if (branchStack.Count > 0)
                    prevAtomIndex = branchStack.Pop();
            }
            else if (c == '-')
            {
                bondOrder = 1;
            }
            else if (c == '=')
            {
                bondOrder = 2;
            }
            else if (c == '#')
            {
                bondOrder = 3;
            }
            else if (char.IsDigit(c))
            {
                var ringNum = c - '0';
                if (ringAtoms.TryGetValue(ringNum, out var ringStart))
                {
                    AddBond(bonds, atomIndex, ringStart, bondOrder);
                    ringAtoms.Remove(ringNum);
                }
                else
                {
                    ringAtoms[ringNum] = atomIndex;
                }
            }
            else if (c == '[')
            {
                var endBracket = smiles.IndexOf(']', i);
                if (endBracket > i)
                {
                    atomIndex++;
                    if (!bonds.ContainsKey(atomIndex))
                        bonds[atomIndex] = new List<(int, int)>();

                    if (prevAtomIndex >= 0)
                    {
                        AddBond(bonds, atomIndex, prevAtomIndex, bondOrder);
                    }
                    prevAtomIndex = atomIndex;
                    bondOrder = 1;
                    i = endBracket;
                }
            }
            else if (char.IsUpper(c))
            {
                atomIndex++;
                if (!bonds.ContainsKey(atomIndex))
                    bonds[atomIndex] = new List<(int, int)>();

                if (prevAtomIndex >= 0)
                {
                    AddBond(bonds, atomIndex, prevAtomIndex, bondOrder);
                }
                prevAtomIndex = atomIndex;
                bondOrder = 1;

                // Skip lowercase part of two-letter element
                if (i + 1 < smiles.Length && char.IsLower(smiles[i + 1]))
                    i++;
            }
        }

        return bonds;
    }

    private void AddBond(
        Dictionary<int, List<(int, int)>> bonds,
        int atom1,
        int atom2,
        int order)
    {
        if (!bonds.ContainsKey(atom1))
            bonds[atom1] = new List<(int, int)>();
        if (!bonds.ContainsKey(atom2))
            bonds[atom2] = new List<(int, int)>();

        bonds[atom1].Add((atom2, order));
        bonds[atom2].Add((atom1, order));
    }

    /// <summary>
    /// Generates a circular feature for an atom at a given radius.
    /// </summary>
    private string GenerateCircularFeature(
        List<string> atoms,
        Dictionary<int, List<(int neighbor, int bondOrder)>> bonds,
        int centerIndex,
        int radius)
    {
        if (centerIndex >= atoms.Count)
            return "";

        var visited = new HashSet<int>();
        var currentLayer = new List<int> { centerIndex };
        var feature = atoms[centerIndex];

        for (int r = 0; r < radius && currentLayer.Count > 0; r++)
        {
            var nextLayer = new List<int>();
            var layerFeatures = new List<string>();

            foreach (var atomIdx in currentLayer)
            {
                visited.Add(atomIdx);

                if (bonds.TryGetValue(atomIdx, out var neighbors))
                {
                    foreach (var (neighbor, bondOrder) in neighbors)
                    {
                        if (!visited.Contains(neighbor) && neighbor < atoms.Count)
                        {
                            nextLayer.Add(neighbor);
                            layerFeatures.Add($"{atoms[neighbor]}{bondOrder}");
                        }
                    }
                }
            }

            if (layerFeatures.Count > 0)
            {
                layerFeatures.Sort();
                feature += ":" + string.Join(",", layerFeatures);
            }

            currentLayer = nextLayer;
        }

        return feature;
    }

    /// <summary>
    /// Adds fingerprint bits for common functional groups.
    /// </summary>
    private void AddFunctionalGroupFeatures(string smiles, HashSet<int> fingerprint)
    {
        var functionalGroups = new Dictionary<string, string[]>
        {
            { "hydroxyl", new[] { "O", "[OH]" } },
            { "carbonyl", new[] { "C=O", "[C]=O" } },
            { "carboxyl", new[] { "C(=O)O", "C(=O)[OH]" } },
            { "amine", new[] { "N", "[NH2]", "[NH]" } },
            { "amide", new[] { "C(=O)N", "NC=O" } },
            { "ether", new[] { "COC", "cOc" } },
            { "ester", new[] { "C(=O)OC" } },
            { "nitro", new[] { "[N+](=O)[O-]", "N(=O)=O" } },
            { "cyano", new[] { "C#N" } },
            { "halogen", new[] { "F", "Cl", "Br", "I" } },
            { "aromatic", new[] { "c1ccccc1", "c1cccc1" } },
            { "sulfide", new[] { "S", "[SH]" } },
            { "phosphate", new[] { "P", "P(=O)" } }
        };

        foreach (var (groupName, patterns) in functionalGroups)
        {
            foreach (var pattern in patterns)
            {
                if (smiles.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    var bit = Math.Abs(groupName.GetHashCode()) % FingerprintSize;
                    fingerprint.Add(bit);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Extracts the element symbol from a bracketed atom.
    /// </summary>
    private string ExtractElementFromBracket(string bracketContent)
    {
        var element = "";
        foreach (var c in bracketContent)
        {
            if (char.IsUpper(c))
            {
                element = c.ToString();
            }
            else if (char.IsLower(c) && element.Length == 1)
            {
                element += c;
                break;
            }
            else if (element.Length > 0)
            {
                break;
            }
        }
        return element.Length > 0 ? element : "C";
    }

    #endregion
}
