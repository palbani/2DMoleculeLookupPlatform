using MoleculeLookup.Core.Enums;

namespace MoleculeLookup.Core.Models;

/// <summary>
/// Represents the result of a molecule search operation.
/// </summary>
public class SearchResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string QuerySmiles { get; set; } = string.Empty;
    public SearchStatus Status { get; set; } = SearchStatus.Pending;
    public bool IsFound { get; set; }
    public MoleculeData? MoleculeData { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan? SearchDuration { get; set; }
}

/// <summary>
/// Result of a similarity search (premium feature).
/// Contains multiple molecules matching the similarity threshold.
/// </summary>
public class SimilaritySearchResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string QuerySmiles { get; set; } = string.Empty;
    public double SimilarityThreshold { get; set; }
    public SearchStatus Status { get; set; } = SearchStatus.Pending;
    public List<SimilarMolecule> SimilarMolecules { get; set; } = new();
    public int TotalMatchCount { get; set; }
    public int DisplayedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan? SearchDuration { get; set; }
}

/// <summary>
/// A molecule similar to the query molecule with its Tanimoto coefficient.
/// </summary>
public class SimilarMolecule
{
    public MoleculeMetadata Metadata { get; set; } = new();
    public double TanimotoCoefficient { get; set; }

    /// <summary>
    /// Full molecule data - loaded lazily via Virtual Proxy pattern.
    /// Null until explicitly fetched by user action.
    /// </summary>
    public MoleculeData? FullData { get; set; }

    /// <summary>
    /// Indicates whether full data has been loaded.
    /// </summary>
    public bool IsFullDataLoaded => FullData != null;
}
