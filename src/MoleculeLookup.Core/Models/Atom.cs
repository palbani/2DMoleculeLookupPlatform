namespace MoleculeLookup.Core.Models;

/// <summary>
/// Represents an atom within a molecule structure.
/// </summary>
public class Atom
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public int AtomicNumber { get; set; }
    public int FormalCharge { get; set; }
    public int ImplicitHydrogens { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsChiralCenter { get; set; }
    public string? ChiralConfiguration { get; set; } // R, S, or null

    /// <summary>
    /// Gets the maximum valence for this atom based on its element.
    /// </summary>
    public int GetMaxValence()
    {
        return Symbol.ToUpper() switch
        {
            "H" => 1,
            "C" => 4,
            "N" => 3,
            "O" => 2,
            "S" => 6,
            "P" => 5,
            "F" => 1,
            "CL" => 1,
            "BR" => 1,
            "I" => 1,
            _ => 4 // Default fallback
        };
    }
}
