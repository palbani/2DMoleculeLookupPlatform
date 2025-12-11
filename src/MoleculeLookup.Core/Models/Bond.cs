namespace MoleculeLookup.Core.Models;

/// <summary>
/// Represents a chemical bond between two atoms.
/// </summary>
public class Bond
{
    public int Id { get; set; }
    public int Atom1Id { get; set; }
    public int Atom2Id { get; set; }
    public BondType Type { get; set; }
    public BondStereo Stereo { get; set; }

    public int Order => Type switch
    {
        BondType.Single => 1,
        BondType.Double => 2,
        BondType.Triple => 3,
        BondType.Aromatic => 1, // Counted as 1.5 in some contexts
        _ => 1
    };
}

public enum BondType
{
    Single = 1,
    Double = 2,
    Triple = 3,
    Aromatic = 4
}

public enum BondStereo
{
    None,
    Up,     // Wedge bond (toward viewer)
    Down,   // Dash bond (away from viewer)
    Either  // Wavy bond (unspecified)
}
