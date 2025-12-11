using MoleculeLookup.Core.Enums;

namespace MoleculeLookup.Core.Models;

/// <summary>
/// Represents an entry in the user's search history library.
/// Used with the Command pattern for undo/redo and history management.
/// </summary>
public class SearchHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SmilesString { get; set; } = string.Empty;
    public string? MoleculeName { get; set; }
    public string? ZincId { get; set; }
    public string? ImageUrl { get; set; }
    public byte[]? ThumbnailImage { get; set; }
    public SearchType SearchType { get; set; }
    public double? SimilarityThreshold { get; set; } // For premium similarity searches
    public SearchStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }
    public bool IsFavorite { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; } // Comma-separated tags

    /// <summary>
    /// Creates a history entry from a search result.
    /// </summary>
    public static SearchHistoryEntry FromSearchResult(SearchResult result)
    {
        return new SearchHistoryEntry
        {
            SmilesString = result.QuerySmiles,
            MoleculeName = result.MoleculeData?.Name,
            ZincId = result.MoleculeData?.ZincId,
            ImageUrl = result.MoleculeData?.ImageUrl,
            SearchType = SearchType.Direct,
            Status = result.Status,
            CreatedAt = result.SearchedAt
        };
    }

    /// <summary>
    /// Creates a history entry from a similarity search.
    /// </summary>
    public static SearchHistoryEntry FromSimilaritySearch(SimilaritySearchResult result)
    {
        return new SearchHistoryEntry
        {
            SmilesString = result.QuerySmiles,
            SearchType = SearchType.Similarity,
            SimilarityThreshold = result.SimilarityThreshold,
            Status = result.Status,
            CreatedAt = result.SearchedAt
        };
    }
}

/// <summary>
/// Type of search performed.
/// </summary>
public enum SearchType
{
    Direct,      // Standard exact SMILES search
    Similarity   // Premium similarity search
}
