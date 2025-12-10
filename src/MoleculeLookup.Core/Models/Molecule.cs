using MoleculeLookup.Core.Enums;

namespace MoleculeLookup.Core.Models;

/// <summary>
/// Represents a validated molecule with its SMILES representation.
/// This is the core entity used for database queries and API calls.
/// </summary>
public class Molecule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SmilesString { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? ZincId { get; set; }
    public string? ImageUrl { get; set; }
    public byte[]? ImageData { get; set; }
    public ValidationStatus ValidationStatus { get; set; } = ValidationStatus.NotValidated;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSearchedAt { get; set; }

    /// <summary>
    /// Creates a Molecule from a drawn structure after validation.
    /// </summary>
    public static Molecule FromDrawnMolecule(DrawnMolecule drawn, string smilesString)
    {
        return new Molecule
        {
            SmilesString = smilesString,
            ValidationStatus = ValidationStatus.Valid,
            CreatedAt = DateTime.UtcNow
        };
    }
}
