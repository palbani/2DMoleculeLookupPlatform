namespace MoleculeLookup.Core.Models;

/// <summary>
/// Lightweight molecule metadata stored in the local database for premium similarity search.
/// Contains only essential data for Tanimoto coefficient calculation.
/// </summary>
public class MoleculeMetadata
{
    public int Id { get; set; }
    public string ZincId { get; set; } = string.Empty;
    public string SmilesString { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Morgan fingerprint bits for Tanimoto similarity calculation.
    /// Stored as a comma-separated string of bit positions for efficiency.
    /// </summary>
    public string FingerprintBits { get; set; } = string.Empty;

    /// <summary>
    /// Number of bits set in the fingerprint (for Tanimoto denominator optimization).
    /// </summary>
    public int FingerprintBitCount { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
