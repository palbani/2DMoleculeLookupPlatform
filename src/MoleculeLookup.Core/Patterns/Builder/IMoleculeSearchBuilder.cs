using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Patterns.Builder;

/// <summary>
/// Interface for the Builder pattern to construct molecule search workflows.
/// Allows step-by-step construction of search operations with different configurations.
/// Both standard and premium versions use the same builder interface.
/// </summary>
public interface IMoleculeSearchBuilder
{
    /// <summary>
    /// Sets the drawn molecule input from the ChemDraw-like interface.
    /// </summary>
    IMoleculeSearchBuilder WithDrawnMolecule(DrawnMolecule molecule);

    /// <summary>
    /// Sets the SMILES string directly (alternative to drawn molecule).
    /// </summary>
    IMoleculeSearchBuilder WithSmiles(string smiles);

    /// <summary>
    /// Enables validation step in the workflow.
    /// </summary>
    IMoleculeSearchBuilder WithValidation();

    /// <summary>
    /// Skips validation step (use with caution).
    /// </summary>
    IMoleculeSearchBuilder SkipValidation();

    /// <summary>
    /// Enables caching of results.
    /// </summary>
    IMoleculeSearchBuilder WithCaching();

    /// <summary>
    /// Enables saving to search history.
    /// </summary>
    IMoleculeSearchBuilder WithHistoryTracking();

    /// <summary>
    /// Sets similarity search threshold (premium feature).
    /// Value between 0.0 and 1.0 representing Tanimoto coefficient.
    /// </summary>
    IMoleculeSearchBuilder WithSimilarityThreshold(double threshold);

    /// <summary>
    /// Sets the maximum number of results to return.
    /// </summary>
    IMoleculeSearchBuilder WithMaxResults(int maxResults);

    /// <summary>
    /// Sets the batch size for loading results (premium feature).
    /// </summary>
    IMoleculeSearchBuilder WithBatchSize(int batchSize);

    /// <summary>
    /// Builds the search workflow and returns the executor.
    /// </summary>
    IMoleculeSearchExecutor Build();

    /// <summary>
    /// Resets the builder to initial state.
    /// </summary>
    IMoleculeSearchBuilder Reset();
}

/// <summary>
/// Interface for executing the built search workflow.
/// </summary>
public interface IMoleculeSearchExecutor
{
    /// <summary>
    /// Executes a standard direct search against ZINC20.
    /// </summary>
    Task<SearchResult> ExecuteDirectSearchAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a similarity search (premium feature).
    /// </summary>
    Task<SimilaritySearchResult> ExecuteSimilaritySearchAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the validation result (if validation was enabled).
    /// </summary>
    ValidationResult? GetValidationResult();

    /// <summary>
    /// Gets the SMILES string used for the search.
    /// </summary>
    string GetSmiles();
}
