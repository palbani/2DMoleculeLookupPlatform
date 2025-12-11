using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Interfaces;

/// <summary>
/// Repository interface for search history management.
/// </summary>
public interface ISearchHistoryRepository
{
    /// <summary>
    /// Gets all search history entries for a user.
    /// </summary>
    Task<IEnumerable<SearchHistoryEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific history entry by ID.
    /// </summary>
    Task<SearchHistoryEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new history entry.
    /// </summary>
    Task<SearchHistoryEntry> AddAsync(SearchHistoryEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing history entry.
    /// </summary>
    Task<SearchHistoryEntry> UpdateAsync(SearchHistoryEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a history entry.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets favorite entries.
    /// </summary>
    Task<IEnumerable<SearchHistoryEntry>> GetFavoritesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches history by molecule name or SMILES.
    /// </summary>
    Task<IEnumerable<SearchHistoryEntry>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all history entries.
    /// </summary>
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
