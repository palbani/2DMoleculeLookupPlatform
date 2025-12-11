using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MoleculeLookup.Core.Interfaces;
using MoleculeLookup.Core.Models;
using MoleculeLookup.Infrastructure.Database;

namespace MoleculeLookup.Infrastructure.Repositories;

/// <summary>
/// Repository for managing search history in the local database.
/// </summary>
public class SearchHistoryRepository : ISearchHistoryRepository
{
    private readonly MoleculeDbContext _context;

    public SearchHistoryRepository(MoleculeDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all search history entries, ordered by creation date descending.
    /// </summary>
    public async Task<IEnumerable<SearchHistoryEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.SearchHistory
            .AsNoTracking()
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToModel());
    }

    /// <summary>
    /// Gets a specific history entry by ID.
    /// </summary>
    public async Task<SearchHistoryEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.SearchHistory
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        return entity?.ToModel();
    }

    /// <summary>
    /// Adds a new history entry.
    /// </summary>
    public async Task<SearchHistoryEntry> AddAsync(SearchHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        var entity = SearchHistoryEntity.FromModel(entry);

        _context.SearchHistory.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.ToModel();
    }

    /// <summary>
    /// Updates an existing history entry.
    /// </summary>
    public async Task<SearchHistoryEntry> UpdateAsync(SearchHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        var entity = await _context.SearchHistory
            .FirstOrDefaultAsync(h => h.Id == entry.Id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"History entry with ID {entry.Id} not found");
        }

        // Update properties
        entity.SmilesString = entry.SmilesString;
        entity.MoleculeName = entry.MoleculeName;
        entity.ZincId = entry.ZincId;
        entity.ImageUrl = entry.ImageUrl;
        entity.ThumbnailImage = entry.ThumbnailImage;
        entity.SearchType = (int)entry.SearchType;
        entity.SimilarityThreshold = entry.SimilarityThreshold;
        entity.Status = (int)entry.Status;
        entity.LastAccessedAt = entry.LastAccessedAt;
        entity.IsFavorite = entry.IsFavorite;
        entity.Notes = entry.Notes;
        entity.Tags = entry.Tags;

        await _context.SaveChangesAsync(cancellationToken);

        return entity.ToModel();
    }

    /// <summary>
    /// Deletes a history entry.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.SearchHistory
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (entity == null)
            return false;

        _context.SearchHistory.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Gets favorite entries.
    /// </summary>
    public async Task<IEnumerable<SearchHistoryEntry>> GetFavoritesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.SearchHistory
            .AsNoTracking()
            .Where(h => h.IsFavorite)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToModel());
    }

    /// <summary>
    /// Searches history by molecule name or SMILES.
    /// </summary>
    public async Task<IEnumerable<SearchHistoryEntry>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.ToLower();

        var entities = await _context.SearchHistory
            .AsNoTracking()
            .Where(h =>
                h.SmilesString.ToLower().Contains(normalizedQuery) ||
                (h.MoleculeName != null && h.MoleculeName.ToLower().Contains(normalizedQuery)) ||
                (h.ZincId != null && h.ZincId.ToLower().Contains(normalizedQuery)) ||
                (h.Tags != null && h.Tags.ToLower().Contains(normalizedQuery)))
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToModel());
    }

    /// <summary>
    /// Clears all history entries.
    /// </summary>
    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        _context.SearchHistory.RemoveRange(_context.SearchHistory);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
