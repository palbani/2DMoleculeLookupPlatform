using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Patterns.Proxy;

/// <summary>
/// Interface for the molecule data proxy.
/// Provides a uniform interface for accessing molecule data,
/// whether it's loaded or needs to be fetched.
/// </summary>
public interface IMoleculeProxy
{
    /// <summary>
    /// Gets the lightweight metadata (always available).
    /// </summary>
    MoleculeMetadata Metadata { get; }

    /// <summary>
    /// Gets the Tanimoto similarity coefficient.
    /// </summary>
    double SimilarityCoefficient { get; }

    /// <summary>
    /// Gets whether the full molecule data has been loaded.
    /// </summary>
    bool IsFullDataLoaded { get; }

    /// <summary>
    /// Gets whether the data is currently being loaded.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets the full molecule data, fetching it if necessary.
    /// </summary>
    Task<MoleculeData?> GetFullDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Preloads the full molecule data without returning it.
    /// Useful for background loading.
    /// </summary>
    Task PreloadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the cached full data to free memory.
    /// </summary>
    void ClearCache();
}
