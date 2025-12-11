using System.Text;
using System.Text.RegularExpressions;
using MoleculeLookup.Core.Interfaces;
using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Infrastructure.Services;

/// <summary>
/// Converts drawn molecules to SMILES (Simplified Molecular Input Line Entry System) strings.
/// SMILES is a line notation for describing the structure of chemical species.
/// </summary>
public class SmilesConverter : ISmilesConverter
{
    // Common organic subset atoms that don't need brackets
    private static readonly HashSet<string> OrganicSubset = new(StringComparer.OrdinalIgnoreCase)
    {
        "B", "C", "N", "O", "P", "S", "F", "Cl", "Br", "I"
    };

    /// <summary>
    /// Converts a drawn molecule to a canonical SMILES string.
    /// Uses depth-first traversal of the molecular graph.
    /// </summary>
    public string ToSmiles(DrawnMolecule molecule)
    {
        if (molecule.Atoms.Count == 0)
            return string.Empty;

        var visited = new HashSet<int>();
        var ringClosures = new Dictionary<(int, int), int>();
        var ringNumber = 1;
        var smiles = new StringBuilder();

        // Start from the first atom (could be optimized to choose a better starting point)
        var startAtom = molecule.Atoms[0];
        BuildSmiles(molecule, startAtom, null, visited, ringClosures, ref ringNumber, smiles);

        return smiles.ToString();
    }

    /// <summary>
    /// Parses a SMILES string back to a molecule structure.
    /// This is a simplified parser for basic SMILES strings.
    /// </summary>
    public DrawnMolecule FromSmiles(string smiles)
    {
        var molecule = new DrawnMolecule();

        if (string.IsNullOrWhiteSpace(smiles))
            return molecule;

        var atomId = 0;
        var atomStack = new Stack<int>();
        var currentAtomId = -1;
        var bondType = BondType.Single;
        var ringAtoms = new Dictionary<int, int>();

        for (int i = 0; i < smiles.Length; i++)
        {
            var c = smiles[i];

            switch (c)
            {
                case '(':
                    if (currentAtomId >= 0)
                        atomStack.Push(currentAtomId);
                    break;

                case ')':
                    if (atomStack.Count > 0)
                        currentAtomId = atomStack.Pop();
                    break;

                case '-':
                    bondType = BondType.Single;
                    break;

                case '=':
                    bondType = BondType.Double;
                    break;

                case '#':
                    bondType = BondType.Triple;
                    break;

                case ':':
                    bondType = BondType.Aromatic;
                    break;

                case '[':
                    // Parse bracketed atom
                    var endBracket = smiles.IndexOf(']', i);
                    if (endBracket > i)
                    {
                        var atomStr = smiles.Substring(i + 1, endBracket - i - 1);
                        var atom = ParseBracketedAtom(atomStr, atomId++);
                        molecule.Atoms.Add(atom);

                        if (currentAtomId >= 0)
                        {
                            molecule.Bonds.Add(new Bond
                            {
                                Id = molecule.Bonds.Count,
                                Atom1Id = currentAtomId,
                                Atom2Id = atom.Id,
                                Type = bondType
                            });
                        }

                        currentAtomId = atom.Id;
                        bondType = BondType.Single;
                        i = endBracket;
                    }
                    break;

                default:
                    if (char.IsLetter(c))
                    {
                        // Check for two-letter elements (Cl, Br)
                        var symbol = c.ToString();
                        if (i + 1 < smiles.Length && char.IsLower(smiles[i + 1]))
                        {
                            symbol += smiles[i + 1];
                            i++;
                        }

                        var newAtom = new Atom
                        {
                            Id = atomId++,
                            Symbol = symbol,
                            AtomicNumber = GetAtomicNumber(symbol)
                        };
                        molecule.Atoms.Add(newAtom);

                        if (currentAtomId >= 0)
                        {
                            molecule.Bonds.Add(new Bond
                            {
                                Id = molecule.Bonds.Count,
                                Atom1Id = currentAtomId,
                                Atom2Id = newAtom.Id,
                                Type = bondType
                            });
                        }

                        currentAtomId = newAtom.Id;
                        bondType = BondType.Single;
                    }
                    else if (char.IsDigit(c))
                    {
                        // Ring closure
                        var ringNum = c - '0';
                        if (ringAtoms.TryGetValue(ringNum, out var ringStartAtom))
                        {
                            molecule.Bonds.Add(new Bond
                            {
                                Id = molecule.Bonds.Count,
                                Atom1Id = ringStartAtom,
                                Atom2Id = currentAtomId,
                                Type = bondType
                            });
                            ringAtoms.Remove(ringNum);
                        }
                        else
                        {
                            ringAtoms[ringNum] = currentAtomId;
                        }
                        bondType = BondType.Single;
                    }
                    break;
            }
        }

        return molecule;
    }

    /// <summary>
    /// Validates that a SMILES string is syntactically correct.
    /// </summary>
    public bool IsValidSmiles(string smiles)
    {
        if (string.IsNullOrWhiteSpace(smiles))
            return false;

        // Basic validation patterns
        var validPattern = @"^[\[\]A-Za-z0-9@+\-=#():\/\\%.]+$";
        if (!Regex.IsMatch(smiles, validPattern))
            return false;

        // Check balanced brackets
        var bracketCount = 0;
        var parenCount = 0;

        foreach (var c in smiles)
        {
            switch (c)
            {
                case '[': bracketCount++; break;
                case ']': bracketCount--; break;
                case '(': parenCount++; break;
                case ')': parenCount--; break;
            }

            if (bracketCount < 0 || parenCount < 0)
                return false;
        }

        return bracketCount == 0 && parenCount == 0;
    }

    /// <summary>
    /// Recursively builds SMILES string using depth-first traversal.
    /// </summary>
    private void BuildSmiles(
        DrawnMolecule molecule,
        Atom atom,
        Bond? incomingBond,
        HashSet<int> visited,
        Dictionary<(int, int), int> ringClosures,
        ref int ringNumber,
        StringBuilder smiles)
    {
        visited.Add(atom.Id);

        // Add atom symbol
        var atomSmiles = GetAtomSmiles(atom, molecule);
        smiles.Append(atomSmiles);

        // Get connected bonds
        var bonds = molecule.GetBondsForAtom(atom.Id)
            .Where(b => b != incomingBond)
            .ToList();

        var branches = new List<(Bond bond, Atom nextAtom)>();

        foreach (var bond in bonds)
        {
            var nextAtomId = bond.Atom1Id == atom.Id ? bond.Atom2Id : bond.Atom1Id;
            var nextAtom = molecule.Atoms.First(a => a.Id == nextAtomId);

            if (visited.Contains(nextAtomId))
            {
                // Ring closure
                var key = (Math.Min(atom.Id, nextAtomId), Math.Max(atom.Id, nextAtomId));
                if (!ringClosures.ContainsKey(key))
                {
                    ringClosures[key] = ringNumber;
                    smiles.Append(GetBondSymbol(bond.Type));
                    smiles.Append(ringNumber);
                    ringNumber++;
                }
            }
            else
            {
                branches.Add((bond, nextAtom));
            }
        }

        // Process branches
        for (int i = 0; i < branches.Count; i++)
        {
            var (bond, nextAtom) = branches[i];
            var bondSymbol = GetBondSymbol(bond.Type);

            if (i < branches.Count - 1)
            {
                // Branch - wrap in parentheses
                smiles.Append('(');
                smiles.Append(bondSymbol);
                BuildSmiles(molecule, nextAtom, bond, visited, ringClosures, ref ringNumber, smiles);
                smiles.Append(')');
            }
            else
            {
                // Main chain - no parentheses
                smiles.Append(bondSymbol);
                BuildSmiles(molecule, nextAtom, bond, visited, ringClosures, ref ringNumber, smiles);
            }
        }
    }

    /// <summary>
    /// Gets the SMILES representation of an atom.
    /// </summary>
    private string GetAtomSmiles(Atom atom, DrawnMolecule molecule)
    {
        var symbol = atom.Symbol;
        var needsBrackets = false;
        var sb = new StringBuilder();

        // Check if atom needs brackets
        if (!OrganicSubset.Contains(symbol))
            needsBrackets = true;

        if (atom.FormalCharge != 0)
            needsBrackets = true;

        if (atom.IsChiralCenter)
            needsBrackets = true;

        if (needsBrackets)
        {
            sb.Append('[');
            sb.Append(symbol);

            // Add chirality
            if (atom.IsChiralCenter && !string.IsNullOrEmpty(atom.ChiralConfiguration))
            {
                sb.Append(atom.ChiralConfiguration == "R" ? "@@" : "@");
            }

            // Add hydrogen count if specified
            if (atom.ImplicitHydrogens > 0)
            {
                sb.Append('H');
                if (atom.ImplicitHydrogens > 1)
                    sb.Append(atom.ImplicitHydrogens);
            }

            // Add charge
            if (atom.FormalCharge != 0)
            {
                if (atom.FormalCharge > 0)
                {
                    sb.Append('+');
                    if (atom.FormalCharge > 1)
                        sb.Append(atom.FormalCharge);
                }
                else
                {
                    sb.Append('-');
                    if (atom.FormalCharge < -1)
                        sb.Append(Math.Abs(atom.FormalCharge));
                }
            }

            sb.Append(']');
        }
        else
        {
            sb.Append(symbol);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets the SMILES bond symbol.
    /// </summary>
    private static string GetBondSymbol(BondType bondType)
    {
        return bondType switch
        {
            BondType.Single => "", // Single bonds are implicit
            BondType.Double => "=",
            BondType.Triple => "#",
            BondType.Aromatic => ":",
            _ => ""
        };
    }

    /// <summary>
    /// Parses a bracketed atom specification like "[NH4+]".
    /// </summary>
    private Atom ParseBracketedAtom(string atomStr, int id)
    {
        var atom = new Atom { Id = id };

        // Extract element symbol (first uppercase + optional lowercase)
        var match = Regex.Match(atomStr, @"^([A-Z][a-z]?)");
        if (match.Success)
        {
            atom.Symbol = match.Groups[1].Value;
            atom.AtomicNumber = GetAtomicNumber(atom.Symbol);
        }

        // Check for chirality
        if (atomStr.Contains("@@"))
        {
            atom.IsChiralCenter = true;
            atom.ChiralConfiguration = "R";
        }
        else if (atomStr.Contains("@"))
        {
            atom.IsChiralCenter = true;
            atom.ChiralConfiguration = "S";
        }

        // Check for hydrogen count
        var hMatch = Regex.Match(atomStr, @"H(\d?)");
        if (hMatch.Success)
        {
            atom.ImplicitHydrogens = string.IsNullOrEmpty(hMatch.Groups[1].Value)
                ? 1
                : int.Parse(hMatch.Groups[1].Value);
        }

        // Check for charge
        var chargeMatch = Regex.Match(atomStr, @"([+-])(\d?)$");
        if (chargeMatch.Success)
        {
            var sign = chargeMatch.Groups[1].Value == "+" ? 1 : -1;
            var magnitude = string.IsNullOrEmpty(chargeMatch.Groups[2].Value)
                ? 1
                : int.Parse(chargeMatch.Groups[2].Value);
            atom.FormalCharge = sign * magnitude;
        }

        return atom;
    }

    /// <summary>
    /// Gets the atomic number for an element symbol.
    /// </summary>
    private static int GetAtomicNumber(string symbol)
    {
        return symbol.ToUpper() switch
        {
            "H" => 1,
            "C" => 6,
            "N" => 7,
            "O" => 8,
            "F" => 9,
            "P" => 15,
            "S" => 16,
            "CL" => 17,
            "BR" => 35,
            "I" => 53,
            _ => 0
        };
    }
}
