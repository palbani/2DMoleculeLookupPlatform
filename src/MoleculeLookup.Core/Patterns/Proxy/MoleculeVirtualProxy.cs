using MoleculeLookup.Core.Interfaces;
using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Patterns.Proxy;

/// <summary>
/// Virtual Proxy implementation for lazy loading molecule data.
///
/// In the premium similarity search, users see many molecule results.
/// Loading full data for all of them would be:
/// - Slow (many API calls)
/// - Wasteful (user may not look at most results)
///
/// The Virtual Proxy pattern solves this by:
/// 1. Showing lightweight metadata (name, image, similarity %) immediately
/// 2. Only fetching full data from ZINC20 when user clicks on a molecule
/// 3. Caching the data once loaded for subsequent access
/// </summary>
public class MoleculeVirtualProxy : IMoleculeProxy
{
    private readonly IZincApiClient _zincApiClient;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private MoleculeData? _fullData;
    private bool _isLoading;
    private Exception? _loadError;

    /// <summary>
    /// Creates a new proxy for a molecule.
    /// </summary>
    /// <param name="metadata">The lightweight metadata (always available)</param>
    /// <param name="similarityCoefficient">The Tanimoto coefficient</param>
    /// <param name="zincApiClient">Client for fetching full data</param>
    public MoleculeVirtualProxy(
        MoleculeMetadata metadata,
        double similarityCoefficient,
        IZincApiClient zincApiClient)
    {
        Metadata = metadata;
        SimilarityCoefficient = similarityCoefficient;
        _zincApiClient = zincApiClient;
    }

    /// <summary>
    /// Gets the lightweight metadata (always available without API call).
    /// </summary>
    public MoleculeMetadata Metadata { get; }

    /// <summary>
    /// Gets the Tanimoto similarity coefficient.
    /// </summary>
    public double SimilarityCoefficient { get; }

    /// <summary>
    /// Gets whether the full molecule data has been loaded.
    /// </summary>
    public bool IsFullDataLoaded => _fullData != null;

    /// <summary>
    /// Gets whether data is currently being loaded.
    /// </summary>
    public bool IsLoading => _isLoading;

    /// <summary>
    /// Gets any error that occurred during loading.
    /// </summary>
    public Exception? LoadError => _loadError;

    /// <summary>
    /// Gets the full molecule data, fetching it from ZINC20 if not already loaded.
    /// This is the core of the Virtual Proxy pattern - lazy loading.
    /// </summary>
    public async Task<MoleculeData?> GetFullDataAsync(CancellationToken cancellationToken = default)
    {
        // Return cached data if available
        if (_fullData != null)
            return _fullData;

        // Use lock to prevent multiple simultaneous fetches
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_fullData != null)
                return _fullData;

            _isLoading = true;
            _loadError = null;

            try
            {
                // Fetch full data from ZINC20 API
                _fullData = await _zincApiClient.GetByZincId(Metadata.ZincId, cancellationToken);
                return _fullData;
            }
            catch (Exception ex)
            {
                _loadError = ex;
                throw;
            }
            finally
            {
                _isLoading = false;
            }
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>
    /// Preloads the full molecule data without returning it.
    /// Useful for background loading when user hovers over a result.
    /// </summary>
    public async Task PreloadAsync(CancellationToken cancellationToken = default)
    {
        if (!IsFullDataLoaded && !IsLoading)
        {
            await GetFullDataAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Clears the cached full data to free memory.
    /// The data will be re-fetched on next access.
    /// </summary>
    public void ClearCache()
    {
        _fullData = null;
        _loadError = null;
    }
}
