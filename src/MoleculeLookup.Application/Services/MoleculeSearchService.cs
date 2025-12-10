using MoleculeLookup.Core.Enums;
using MoleculeLookup.Core.Interfaces;
using MoleculeLookup.Core.Models;
using MoleculeLookup.Core.Patterns.Builder;
using MoleculeLookup.Core.Patterns.Command;
using MoleculeLookup.Core.Patterns.Proxy;

namespace MoleculeLookup.Application.Services;

/// <summary>
/// Main application service that orchestrates molecule search operations.
/// Provides a high-level API for both standard and premium users.
/// </summary>
public class MoleculeSearchService
{
    private readonly IMoleculeValidator _validator;
    private readonly ISmilesConverter _smilesConverter;
    private readonly IZincApiClient _zincApiClient;
    private readonly ITanimotoCalculator? _tanimotoCalculator;
    private readonly IMoleculeMetadataRepository? _metadataRepository;
    private readonly ISearchHistoryRepository _historyRepository;
    private readonly SearchCommandInvoker _commandInvoker;
    private readonly SubscriptionTier _subscriptionTier;

    /// <summary>
    /// Creates a standard version service (no similarity search).
    /// </summary>
    public MoleculeSearchService(
        IMoleculeValidator validator,
        ISmilesConverter smilesConverter,
        IZincApiClient zincApiClient,
        ISearchHistoryRepository historyRepository)
    {
        _validator = validator;
        _smilesConverter = smilesConverter;
        _zincApiClient = zincApiClient;
        _historyRepository = historyRepository;
        _commandInvoker = new SearchCommandInvoker();
        _subscriptionTier = SubscriptionTier.Standard;
    }

    /// <summary>
    /// Creates a premium version service (includes similarity search).
    /// </summary>
    public MoleculeSearchService(
        IMoleculeValidator validator,
        ISmilesConverter smilesConverter,
        IZincApiClient zincApiClient,
        ITanimotoCalculator tanimotoCalculator,
        IMoleculeMetadataRepository metadataRepository,
        ISearchHistoryRepository historyRepository)
        : this(validator, smilesConverter, zincApiClient, historyRepository)
    {
        _tanimotoCalculator = tanimotoCalculator;
        _metadataRepository = metadataRepository;
        _subscriptionTier = SubscriptionTier.Premium;
    }

    /// <summary>
    /// Gets the current subscription tier.
    /// </summary>
    public SubscriptionTier SubscriptionTier => _subscriptionTier;

    /// <summary>
    /// Gets whether similarity search is available.
    /// </summary>
    public bool IsSimilaritySearchAvailable =>
        _subscriptionTier == SubscriptionTier.Premium &&
        _tanimotoCalculator != null &&
        _metadataRepository != null;

    /// <summary>
    /// Gets the command invoker for undo/redo operations.
    /// </summary>
    public SearchCommandInvoker CommandInvoker => _commandInvoker;

    #region Standard Search Operations

    /// <summary>
    /// Validates a drawn molecule structure.
    /// </summary>
    public ValidationResult ValidateMolecule(DrawnMolecule molecule)
    {
        return _validator.Validate(molecule);
    }

    /// <summary>
    /// Converts a drawn molecule to SMILES string.
    /// </summary>
    public string ConvertToSmiles(DrawnMolecule molecule)
    {
        return _smilesConverter.ToSmiles(molecule);
    }

    /// <summary>
    /// Performs a direct search on ZINC20 database using a drawn molecule.
    /// </summary>
    public async Task<SearchResult> SearchAsync(
        DrawnMolecule molecule,
        bool saveToHistory = true,
        CancellationToken cancellationToken = default)
    {
        var builder = CreateSearchBuilder()
            .WithDrawnMolecule(molecule)
            .WithValidation();

        if (saveToHistory)
        {
            builder.WithHistoryTracking();
        }

        var executor = builder.Build();
        var result = await executor.ExecuteDirectSearchAsync(cancellationToken);

        // Add to history using command pattern
        if (saveToHistory && result.IsFound)
        {
            var historyEntry = SearchHistoryEntry.FromSearchResult(result);
            var command = new AddToHistoryCommand(_historyRepository, historyEntry);
            await _commandInvoker.ExecuteAsync(command, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Performs a direct search using a SMILES string.
    /// </summary>
    public async Task<SearchResult> SearchBySmilesAsync(
        string smiles,
        bool saveToHistory = true,
        CancellationToken cancellationToken = default)
    {
        var executor = CreateSearchBuilder()
            .WithSmiles(smiles)
            .Build();

        var result = await executor.ExecuteDirectSearchAsync(cancellationToken);

        if (saveToHistory && result.IsFound)
        {
            var historyEntry = SearchHistoryEntry.FromSearchResult(result);
            var command = new AddToHistoryCommand(_historyRepository, historyEntry);
            await _commandInvoker.ExecuteAsync(command, cancellationToken);
        }

        return result;
    }

    #endregion

    #region Premium Similarity Search Operations

    /// <summary>
    /// Performs a similarity search (premium feature).
    /// </summary>
    /// <param name="molecule">The query molecule</param>
    /// <param name="similarityThreshold">Minimum Tanimoto coefficient (0.0 to 1.0)</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="batchSize">Batch size for result pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<SimilaritySearchResult> SimilaritySearchAsync(
        DrawnMolecule molecule,
        double similarityThreshold = 0.8,
        int maxResults = 100,
        int batchSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePremiumFeature();

        var executor = CreatePremiumSearchBuilder()
            .WithDrawnMolecule(molecule)
            .WithValidation()
            .WithSimilarityThreshold(similarityThreshold)
            .WithMaxResults(maxResults)
            .WithBatchSize(batchSize)
            .WithHistoryTracking()
            .Build();

        var result = await executor.ExecuteSimilaritySearchAsync(cancellationToken);

        // Save to history
        if (result.Status == SearchStatus.Completed)
        {
            var historyEntry = SearchHistoryEntry.FromSimilaritySearch(result);
            var command = new AddToHistoryCommand(_historyRepository, historyEntry);
            await _commandInvoker.ExecuteAsync(command, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Performs a similarity search using a SMILES string (premium feature).
    /// </summary>
    public async Task<SimilaritySearchResult> SimilaritySearchBySmilesAsync(
        string smiles,
        double similarityThreshold = 0.8,
        int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        EnsurePremiumFeature();

        var executor = CreatePremiumSearchBuilder()
            .WithSmiles(smiles)
            .WithSimilarityThreshold(similarityThreshold)
            .WithMaxResults(maxResults)
            .WithHistoryTracking()
            .Build();

        return await executor.ExecuteSimilaritySearchAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a proxy collection for lazy loading similarity results.
    /// </summary>
    public MoleculeProxyCollection CreateProxyCollection(
        SimilaritySearchResult result,
        int batchSize = 50)
    {
        var collection = new MoleculeProxyCollection(_zincApiClient, batchSize);
        collection.AddFromSimilarityResult(result.SimilarMolecules);
        return collection;
    }

    /// <summary>
    /// Gets full molecule data for a similar molecule (triggers API call via proxy).
    /// </summary>
    public async Task<MoleculeData?> GetFullMoleculeDataAsync(
        string zincId,
        CancellationToken cancellationToken = default)
    {
        return await _zincApiClient.GetByZincId(zincId, cancellationToken);
    }

    #endregion

    #region History Operations

    /// <summary>
    /// Gets all search history entries.
    /// </summary>
    public async Task<IEnumerable<SearchHistoryEntry>> GetHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        return await _historyRepository.GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// Gets favorite history entries.
    /// </summary>
    public async Task<IEnumerable<SearchHistoryEntry>> GetFavoritesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _historyRepository.GetFavoritesAsync(cancellationToken);
    }

    /// <summary>
    /// Toggles favorite status for a history entry.
    /// </summary>
    public async Task<SearchHistoryEntry?> ToggleFavoriteAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var command = new ToggleFavoriteCommand(_historyRepository, entryId);
        await _commandInvoker.ExecuteAsync(command, cancellationToken);
        return command.Result;
    }

    /// <summary>
    /// Adds notes to a history entry.
    /// </summary>
    public async Task<SearchHistoryEntry?> AddNotesAsync(
        Guid entryId,
        string notes,
        CancellationToken cancellationToken = default)
    {
        var command = new AddNotesCommand(_historyRepository, entryId, notes);
        await _commandInvoker.ExecuteAsync(command, cancellationToken);
        return command.Result;
    }

    /// <summary>
    /// Deletes a history entry.
    /// </summary>
    public async Task<bool> DeleteHistoryEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteFromHistoryCommand(_historyRepository, entryId);
        await _commandInvoker.ExecuteAsync(command, cancellationToken);
        return command.Result ?? false;
    }

    /// <summary>
    /// Re-runs a search from history.
    /// </summary>
    public async Task<SearchResult> RerunSearchAsync(
        Guid historyEntryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _historyRepository.GetByIdAsync(historyEntryId, cancellationToken);
        if (entry == null)
        {
            return new SearchResult
            {
                Status = SearchStatus.Failed,
                ErrorMessage = "History entry not found"
            };
        }

        // Update last accessed time
        entry.LastAccessedAt = DateTime.UtcNow;
        await _historyRepository.UpdateAsync(entry, cancellationToken);

        return await SearchBySmilesAsync(entry.SmilesString, false, cancellationToken);
    }

    /// <summary>
    /// Undoes the last history operation.
    /// </summary>
    public async Task<bool> UndoAsync(CancellationToken cancellationToken = default)
    {
        return await _commandInvoker.UndoAsync(cancellationToken);
    }

    /// <summary>
    /// Redoes the last undone operation.
    /// </summary>
    public async Task<bool> RedoAsync(CancellationToken cancellationToken = default)
    {
        return await _commandInvoker.RedoAsync(cancellationToken);
    }

    #endregion

    #region Private Methods

    private IMoleculeSearchBuilder CreateSearchBuilder()
    {
        return new MoleculeSearchBuilder(
            _validator,
            _smilesConverter,
            _zincApiClient,
            _historyRepository);
    }

    private IMoleculeSearchBuilder CreatePremiumSearchBuilder()
    {
        EnsurePremiumFeature();

        return new MoleculeSearchBuilder(
            _validator,
            _smilesConverter,
            _zincApiClient,
            _tanimotoCalculator!,
            _metadataRepository!,
            _historyRepository);
    }

    private void EnsurePremiumFeature()
    {
        if (!IsSimilaritySearchAvailable)
        {
            throw new InvalidOperationException(
                "Similarity search is a premium feature. " +
                "Please upgrade to the premium version to access this functionality.");
        }
    }

    #endregion
}
