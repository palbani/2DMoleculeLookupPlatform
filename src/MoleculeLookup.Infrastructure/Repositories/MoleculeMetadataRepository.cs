using Microsoft.EntityFrameworkCore;
using MoleculeLookup.Core.Interfaces;
using MoleculeLookup.Core.Models;
using MoleculeLookup.Infrastructure.Database;

namespace MoleculeLookup.Infrastructure.Repositories;

/// <summary>
/// Repository for accessing molecule metadata stored in the local database.
/// Used for premium similarity search feature.
/// </summary>
public class MoleculeMetadataRepository : IMoleculeMetadataRepository
{
    private readonly MoleculeDbContext _context;

    public MoleculeMetadataRepository(MoleculeDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all molecule metadata for similarity search.
    /// </summary>
    public async Task<IEnumerable<MoleculeMetadata>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.MoleculeMetadata
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToModel());
    }

    /// <summary>
    /// Gets molecule metadata by ZINC ID.
    /// </summary>
    public async Task<MoleculeMetadata?> GetByZincIdAsync(string zincId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.MoleculeMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ZincId == zincId, cancellationToken);

        return entity?.ToModel();
    }

    /// <summary>
    /// Gets molecule metadata by SMILES string.
    /// </summary>
    public async Task<MoleculeMetadata?> GetBySmilesAsync(string smiles, CancellationToken cancellationToken = default)
    {
        var entity = await _context.MoleculeMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.SmilesString == smiles, cancellationToken);

        return entity?.ToModel();
    }

    /// <summary>
    /// Adds molecule metadata to the database.
    /// </summary>
    public async Task<MoleculeMetadata> AddAsync(MoleculeMetadata metadata, CancellationToken cancellationToken = default)
    {
        var entity = MoleculeMetadataEntity.FromModel(metadata);

        _context.MoleculeMetadata.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.ToModel();
    }

    /// <summary>
    /// Gets the total count of molecules in the database.
    /// </summary>
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MoleculeMetadata.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Gets molecules in batches for pagination.
    /// </summary>
    public async Task<IEnumerable<MoleculeMetadata>> GetBatchAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var entities = await _context.MoleculeMetadata
            .AsNoTracking()
            .OrderBy(m => m.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToModel());
    }

    /// <summary>
    /// Bulk adds molecule metadata for database seeding.
    /// </summary>
    public async Task BulkAddAsync(IEnumerable<MoleculeMetadata> metadata, CancellationToken cancellationToken = default)
    {
        var entities = metadata.Select(MoleculeMetadataEntity.FromModel);
        await _context.MoleculeMetadata.AddRangeAsync(entities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
