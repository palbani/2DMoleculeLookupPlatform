namespace MoleculeLookup.Core.Models;

/// <summary>
/// Represents a molecule structure drawn by the user in the ChemDraw-like interface.
/// This is the input from the drawing tool before conversion to SMILES.
/// </summary>
public class DrawnMolecule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<Atom> Atoms { get; set; } = new();
    public List<Bond> Bonds { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the total formal charge of the molecule.
    /// </summary>
    public int TotalCharge => Atoms.Sum(a => a.FormalCharge);

    /// <summary>
    /// Gets atoms connected to the specified atom.
    /// </summary>
    public IEnumerable<Atom> GetConnectedAtoms(int atomId)
    {
        var connectedIds = Bonds
            .Where(b => b.Atom1Id == atomId || b.Atom2Id == atomId)
            .Select(b => b.Atom1Id == atomId ? b.Atom2Id : b.Atom1Id);

        return Atoms.Where(a => connectedIds.Contains(a.Id));
    }

    /// <summary>
    /// Gets bonds connected to the specified atom.
    /// </summary>
    public IEnumerable<Bond> GetBondsForAtom(int atomId)
    {
        return Bonds.Where(b => b.Atom1Id == atomId || b.Atom2Id == atomId);
    }

    /// <summary>
    /// Calculates the current valence of an atom based on its bonds.
    /// </summary>
    public int GetAtomValence(int atomId)
    {
        var atom = Atoms.FirstOrDefault(a => a.Id == atomId);
        if (atom == null) return 0;

        var bondOrders = GetBondsForAtom(atomId).Sum(b => b.Order);
        return bondOrders + atom.ImplicitHydrogens;
    }

    /// <summary>
    /// Gets atoms that are chiral centers.
    /// </summary>
    public IEnumerable<Atom> GetChiralCenters()
    {
        return Atoms.Where(a => a.IsChiralCenter);
    }
}
