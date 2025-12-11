using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Interfaces;

/// <summary>
/// Repository interface for molecule metadata (premium feature database).
/// </summary>
public interface IMoleculeMetadataRepository
{
    /// <summary>
    /// Gets all molecule metadata for similarity search.
    /// </summary>
    Task<IEnumerable<MoleculeMetadata>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets molecule metadata by ZINC ID.
    /// </summary>
    Task<MoleculeMetadata?> GetByZincIdAsync(string zincId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets molecule metadata by SMILES string.
    /// </summary>
    Task<MoleculeMetadata?> GetBySmilesAsync(string smiles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds molecule metadata to the database.
    /// </summary>
    Task<MoleculeMetadata> AddAsync(MoleculeMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of molecules in the database.
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets molecules in batches for pagination.
    /// </summary>
    Task<IEnumerable<MoleculeMetadata>> GetBatchAsync(int skip, int take, CancellationToken cancellationToken = default);
}
