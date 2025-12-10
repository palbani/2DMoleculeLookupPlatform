using MoleculeLookup.Core.Interfaces;
using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Patterns.Proxy;

/// <summary>
/// Collection of molecule proxies with batch loading capabilities.
/// Manages multiple molecule proxies and supports:
/// - Pagination (batch loading)
/// - Background preloading
/// - Memory management (clearing old cached data)
/// </summary>
public class MoleculeProxyCollection
{
    private readonly List<MoleculeVirtualProxy> _proxies = new();
    private readonly IZincApiClient _zincApiClient;
    private readonly int _batchSize;

    /// <summary>
    /// Event raised when a batch of molecules starts loading.
    /// </summary>
    public event EventHandler<BatchLoadEventArgs>? BatchLoadStarted;

    /// <summary>
    /// Event raised when a batch of molecules finishes loading.
    /// </summary>
    public event EventHandler<BatchLoadEventArgs>? BatchLoadCompleted;

    /// <summary>
    /// Event raised when a single molecule's full data is loaded.
    /// </summary>
    public event EventHandler<MoleculeLoadedEventArgs>? MoleculeLoaded;

    /// <summary>
    /// Creates a new proxy collection.
    /// </summary>
    /// <param name="zincApiClient">Client for fetching full data</param>
    /// <param name="batchSize">Number of molecules to load per batch (5-500)</param>
    public MoleculeProxyCollection(IZincApiClient zincApiClient, int batchSize = 50)
    {
        _zincApiClient = zincApiClient;
        _batchSize = Math.Clamp(batchSize, 5, 500);
    }

    /// <summary>
    /// Gets the total number of molecules in the collection.
    /// </summary>
    public int TotalCount => _proxies.Count;

    /// <summary>
    /// Gets the number of molecules with loaded full data.
    /// </summary>
    public int LoadedCount => _proxies.Count(p => p.IsFullDataLoaded);

    /// <summary>
    /// Gets the configured batch size.
    /// </summary>
    public int BatchSize => _batchSize;

    /// <summary>
    /// Gets the total number of batches.
    /// </summary>
    public int TotalBatches => (int)Math.Ceiling((double)_proxies.Count / _batchSize);

    /// <summary>
    /// Adds molecules from a similarity search result.
    /// </summary>
    public void AddFromSimilarityResult(IEnumerable<SimilarMolecule> similarMolecules)
    {
        foreach (var similar in similarMolecules)
        {
            var proxy = new MoleculeVirtualProxy(
                similar.Metadata,
                similar.TanimotoCoefficient,
                _zincApiClient);

            _proxies.Add(proxy);
        }
    }

    /// <summary>
    /// Gets a batch of molecules by batch index.
    /// </summary>
    public IEnumerable<MoleculeVirtualProxy> GetBatch(int batchIndex)
    {
        var skip = batchIndex * _batchSize;
        return _proxies.Skip(skip).Take(_batchSize);
    }

    /// <summary>
    /// Gets a specific molecule proxy by index.
    /// </summary>
    public MoleculeVirtualProxy? GetByIndex(int index)
    {
        if (index < 0 || index >= _proxies.Count)
            return null;

        return _proxies[index];
    }

    /// <summary>
    /// Gets a molecule proxy by ZINC ID.
    /// </summary>
    public MoleculeVirtualProxy? GetByZincId(string zincId)
    {
        return _proxies.FirstOrDefault(p =>
            p.Metadata.ZincId.Equals(zincId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Loads full data for a batch of molecules.
    /// </summary>
    public async Task LoadBatchAsync(int batchIndex, CancellationToken cancellationToken = default)
    {
        var batch = GetBatch(batchIndex).ToList();

        BatchLoadStarted?.Invoke(this, new BatchLoadEventArgs(batchIndex, batch.Count));

        var loadTasks = batch
            .Where(p => !p.IsFullDataLoaded && !p.IsLoading)
            .Select(async proxy =>
            {
                try
                {
                    await proxy.GetFullDataAsync(cancellationToken);
                    MoleculeLoaded?.Invoke(this, new MoleculeLoadedEventArgs(proxy));
                }
                catch
                {
                    // Log error but continue loading other molecules
                }
            });

        await Task.WhenAll(loadTasks);

        BatchLoadCompleted?.Invoke(this, new BatchLoadEventArgs(batchIndex, batch.Count));
    }

    /// <summary>
    /// Preloads the next batch of molecules in the background.
    /// Useful for anticipating user scroll.
    /// </summary>
    public Task PreloadNextBatchAsync(int currentBatchIndex, CancellationToken cancellationToken = default)
    {
        var nextBatchIndex = currentBatchIndex + 1;
        if (nextBatchIndex < TotalBatches)
        {
            return LoadBatchAsync(nextBatchIndex, cancellationToken);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears cached full data for all molecules to free memory.
    /// </summary>
    public void ClearAllCaches()
    {
        foreach (var proxy in _proxies)
        {
            proxy.ClearCache();
        }
    }

    /// <summary>
    /// Clears cached data for molecules outside the visible range.
    /// Keeps data for current batch and adjacent batches.
    /// </summary>
    public void ClearDistantCaches(int currentBatchIndex)
    {
        for (int i = 0; i < TotalBatches; i++)
        {
            // Keep current batch and one batch on each side
            if (Math.Abs(i - currentBatchIndex) > 1)
            {
                foreach (var proxy in GetBatch(i))
                {
                    proxy.ClearCache();
                }
            }
        }
    }

    /// <summary>
    /// Gets all molecule proxies (for iteration).
    /// </summary>
    public IEnumerable<MoleculeVirtualProxy> GetAll() => _proxies;

    /// <summary>
    /// Gets proxies sorted by similarity coefficient (descending).
    /// </summary>
    public IEnumerable<MoleculeVirtualProxy> GetSortedBySimilarity()
    {
        return _proxies.OrderByDescending(p => p.SimilarityCoefficient);
    }

    /// <summary>
    /// Filters proxies by minimum similarity threshold.
    /// </summary>
    public IEnumerable<MoleculeVirtualProxy> FilterBySimilarity(double minSimilarity)
    {
        return _proxies.Where(p => p.SimilarityCoefficient >= minSimilarity);
    }

    /// <summary>
    /// Clears the entire collection.
    /// </summary>
    public void Clear()
    {
        ClearAllCaches();
        _proxies.Clear();
    }
}

/// <summary>
/// Event args for batch loading events.
/// </summary>
public class BatchLoadEventArgs : EventArgs
{
    public int BatchIndex { get; }
    public int MoleculeCount { get; }

    public BatchLoadEventArgs(int batchIndex, int moleculeCount)
    {
        BatchIndex = batchIndex;
        MoleculeCount = moleculeCount;
    }
}

/// <summary>
/// Event args for individual molecule loading.
/// </summary>
public class MoleculeLoadedEventArgs : EventArgs
{
    public MoleculeVirtualProxy Proxy { get; }

    public MoleculeLoadedEventArgs(MoleculeVirtualProxy proxy)
    {
        Proxy = proxy;
    }
}
