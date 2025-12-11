using MoleculeLookup.Core.Enums;
using MoleculeLookup.Core.Interfaces;
using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Patterns.Builder;

/// <summary>
/// Builder pattern implementation for constructing molecule search workflows.
/// Allows flexible configuration of the search process with method chaining.
///
/// Example usage:
/// var executor = builder
///     .WithDrawnMolecule(molecule)
///     .WithValidation()
///     .WithCaching()
///     .WithHistoryTracking()
///     .Build();
/// var result = await executor.ExecuteDirectSearchAsync();
/// </summary>
public class MoleculeSearchBuilder : IMoleculeSearchBuilder
{
    private readonly IMoleculeValidator _validator;
    private readonly ISmilesConverter _smilesConverter;
    private readonly IZincApiClient _zincApiClient;
    private readonly ITanimotoCalculator? _tanimotoCalculator;
    private readonly IMoleculeMetadataRepository? _metadataRepository;
    private readonly ISearchHistoryRepository? _historyRepository;

    // Builder state
    private DrawnMolecule? _drawnMolecule;
    private string? _smiles;
    private bool _enableValidation = true;
    private bool _enableCaching = false;
    private bool _enableHistoryTracking = false;
    private double? _similarityThreshold;
    private int _maxResults = 100;
    private int _batchSize = 50;

    /// <summary>
    /// Creates a builder for standard version (no similarity search).
    /// </summary>
    public MoleculeSearchBuilder(
        IMoleculeValidator validator,
        ISmilesConverter smilesConverter,
        IZincApiClient zincApiClient,
        ISearchHistoryRepository? historyRepository = null)
    {
        _validator = validator;
        _smilesConverter = smilesConverter;
        _zincApiClient = zincApiClient;
        _historyRepository = historyRepository;
    }

    /// <summary>
    /// Creates a builder for premium version (includes similarity search).
    /// </summary>
    public MoleculeSearchBuilder(
        IMoleculeValidator validator,
        ISmilesConverter smilesConverter,
        IZincApiClient zincApiClient,
        ITanimotoCalculator tanimotoCalculator,
        IMoleculeMetadataRepository metadataRepository,
        ISearchHistoryRepository? historyRepository = null)
        : this(validator, smilesConverter, zincApiClient, historyRepository)
    {
        _tanimotoCalculator = tanimotoCalculator;
        _metadataRepository = metadataRepository;
    }

    public IMoleculeSearchBuilder WithDrawnMolecule(DrawnMolecule molecule)
    {
        _drawnMolecule = molecule;
        _smiles = null; // Clear SMILES if drawn molecule is set
        return this;
    }

    public IMoleculeSearchBuilder WithSmiles(string smiles)
    {
        _smiles = smiles;
        _drawnMolecule = null; // Clear drawn molecule if SMILES is set
        return this;
    }

    public IMoleculeSearchBuilder WithValidation()
    {
        _enableValidation = true;
        return this;
    }

    public IMoleculeSearchBuilder SkipValidation()
    {
        _enableValidation = false;
        return this;
    }

    public IMoleculeSearchBuilder WithCaching()
    {
        _enableCaching = true;
        return this;
    }

    public IMoleculeSearchBuilder WithHistoryTracking()
    {
        _enableHistoryTracking = true;
        return this;
    }

    public IMoleculeSearchBuilder WithSimilarityThreshold(double threshold)
    {
        if (threshold < 0.0 || threshold > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold),
                "Similarity threshold must be between 0.0 and 1.0");
        }

        _similarityThreshold = threshold;
        return this;
    }

    public IMoleculeSearchBuilder WithMaxResults(int maxResults)
    {
        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxResults),
                "Max results must be greater than 0");
        }

        _maxResults = maxResults;
        return this;
    }

    public IMoleculeSearchBuilder WithBatchSize(int batchSize)
    {
        if (batchSize < 5 || batchSize > 500)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                "Batch size must be between 5 and 500");
        }

        _batchSize = batchSize;
        return this;
    }

    public IMoleculeSearchExecutor Build()
    {
        // Validate that we have input
        if (_drawnMolecule == null && string.IsNullOrEmpty(_smiles))
        {
            throw new InvalidOperationException(
                "Either a drawn molecule or SMILES string must be provided");
        }

        // Create the search configuration
        var config = new SearchConfiguration
        {
            DrawnMolecule = _drawnMolecule,
            Smiles = _smiles,
            EnableValidation = _enableValidation,
            EnableCaching = _enableCaching,
            EnableHistoryTracking = _enableHistoryTracking,
            SimilarityThreshold = _similarityThreshold,
            MaxResults = _maxResults,
            BatchSize = _batchSize
        };

        // Create and return the executor
        return new MoleculeSearchExecutor(
            config,
            _validator,
            _smilesConverter,
            _zincApiClient,
            _tanimotoCalculator,
            _metadataRepository,
            _historyRepository);
    }

    public IMoleculeSearchBuilder Reset()
    {
        _drawnMolecule = null;
        _smiles = null;
        _enableValidation = true;
        _enableCaching = false;
        _enableHistoryTracking = false;
        _similarityThreshold = null;
        _maxResults = 100;
        _batchSize = 50;
        return this;
    }
}

/// <summary>
/// Configuration object created by the builder.
/// </summary>
internal class SearchConfiguration
{
    public DrawnMolecule? DrawnMolecule { get; set; }
    public string? Smiles { get; set; }
    public bool EnableValidation { get; set; }
    public bool EnableCaching { get; set; }
    public bool EnableHistoryTracking { get; set; }
    public double? SimilarityThreshold { get; set; }
    public int MaxResults { get; set; }
    public int BatchSize { get; set; }
}

/// <summary>
/// Executor that performs the actual search based on built configuration.
/// </summary>
internal class MoleculeSearchExecutor : IMoleculeSearchExecutor
{
    private readonly SearchConfiguration _config;
    private readonly IMoleculeValidator _validator;
    private readonly ISmilesConverter _smilesConverter;
    private readonly IZincApiClient _zincApiClient;
    private readonly ITanimotoCalculator? _tanimotoCalculator;
    private readonly IMoleculeMetadataRepository? _metadataRepository;
    private readonly ISearchHistoryRepository? _historyRepository;

    private ValidationResult? _validationResult;
    private string? _processedSmiles;

    public MoleculeSearchExecutor(
        SearchConfiguration config,
        IMoleculeValidator validator,
        ISmilesConverter smilesConverter,
        IZincApiClient zincApiClient,
        ITanimotoCalculator? tanimotoCalculator,
        IMoleculeMetadataRepository? metadataRepository,
        ISearchHistoryRepository? historyRepository)
    {
        _config = config;
        _validator = validator;
        _smilesConverter = smilesConverter;
        _zincApiClient = zincApiClient;
        _tanimotoCalculator = tanimotoCalculator;
        _metadataRepository = metadataRepository;
        _historyRepository = historyRepository;
    }

    public async Task<SearchResult> ExecuteDirectSearchAsync(CancellationToken cancellationToken = default)
    {
        var result = new SearchResult
        {
            Status = SearchStatus.InProgress,
            SearchedAt = DateTime.UtcNow
        };

        try
        {
            // Step 1: Get SMILES string (convert from drawn molecule if needed)
            _processedSmiles = await GetSmilesAsync();
            result.QuerySmiles = _processedSmiles;

            // Step 2: Validate if enabled
            if (_config.EnableValidation && _config.DrawnMolecule != null)
            {
                _validationResult = _validator.Validate(_config.DrawnMolecule);
                if (!_validationResult.IsValid)
                {
                    result.Status = SearchStatus.Failed;
                    result.ErrorMessage = $"Validation failed: {_validationResult.ErrorMessage}";
                    return result;
                }
            }

            // Step 3: Execute search against ZINC20 API
            result = await _zincApiClient.SearchBySmiles(_processedSmiles, cancellationToken);

            // Step 4: Save to history if enabled
            if (_config.EnableHistoryTracking && _historyRepository != null && result.IsFound)
            {
                var historyEntry = SearchHistoryEntry.FromSearchResult(result);
                await _historyRepository.AddAsync(historyEntry, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            result.Status = SearchStatus.Failed;
            result.ErrorMessage = $"Search failed: {ex.Message}";
        }

        return result;
    }

    public async Task<SimilaritySearchResult> ExecuteSimilaritySearchAsync(CancellationToken cancellationToken = default)
    {
        var result = new SimilaritySearchResult
        {
            Status = SearchStatus.InProgress,
            SearchedAt = DateTime.UtcNow,
            SimilarityThreshold = _config.SimilarityThreshold ?? 0.8
        };

        try
        {
            // Validate that premium features are available
            if (_tanimotoCalculator == null || _metadataRepository == null)
            {
                throw new InvalidOperationException(
                    "Similarity search requires premium features. " +
                    "Please ensure TanimotoCalculator and MetadataRepository are configured.");
            }

            // Step 1: Get SMILES string
            _processedSmiles = await GetSmilesAsync();
            result.QuerySmiles = _processedSmiles;

            // Step 2: Validate if enabled
            if (_config.EnableValidation && _config.DrawnMolecule != null)
            {
                _validationResult = _validator.Validate(_config.DrawnMolecule);
                if (!_validationResult.IsValid)
                {
                    result.Status = SearchStatus.Failed;
                    result.ErrorMessage = $"Validation failed: {_validationResult.ErrorMessage}";
                    return result;
                }
            }

            // Step 3: Get molecules from local database
            var candidates = await _metadataRepository.GetAllAsync(cancellationToken);

            // Step 4: Calculate Tanimoto similarity
            var similarMolecules = _tanimotoCalculator.FindSimilar(
                _processedSmiles,
                candidates,
                result.SimilarityThreshold);

            // Step 5: Apply batch size limit
            result.TotalMatchCount = similarMolecules.Count();
            result.SimilarMolecules = similarMolecules
                .OrderByDescending(m => m.TanimotoCoefficient)
                .Take(_config.MaxResults)
                .ToList();
            result.DisplayedCount = result.SimilarMolecules.Count;

            result.Status = SearchStatus.Completed;

            // Step 6: Save to history if enabled
            if (_config.EnableHistoryTracking && _historyRepository != null)
            {
                var historyEntry = SearchHistoryEntry.FromSimilaritySearch(result);
                await _historyRepository.AddAsync(historyEntry, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            result.Status = SearchStatus.Failed;
            result.ErrorMessage = $"Similarity search failed: {ex.Message}";
        }

        return result;
    }

    public ValidationResult? GetValidationResult() => _validationResult;

    public string GetSmiles() => _processedSmiles ?? string.Empty;

    private Task<string> GetSmilesAsync()
    {
        if (!string.IsNullOrEmpty(_config.Smiles))
        {
            return Task.FromResult(_config.Smiles);
        }

        if (_config.DrawnMolecule != null)
        {
            var smiles = _smilesConverter.ToSmiles(_config.DrawnMolecule);
            return Task.FromResult(smiles);
        }

        throw new InvalidOperationException("No molecule input provided");
    }
}
