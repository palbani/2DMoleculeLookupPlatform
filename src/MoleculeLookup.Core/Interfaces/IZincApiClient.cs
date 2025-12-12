using System.Threading;
using System.Threading.Tasks;
using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Interfaces;

/// <summary>
/// Interface for ZINC20 database API client.
/// </summary>
public interface IZincApiClient
{
    /// <summary>
    /// Searches for a molecule by its SMILES string.
    /// </summary>
    Task<SearchResult> SearchBySmiles(string smiles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets full molecule data by ZINC ID.
    /// </summary>
    Task<MoleculeData?> GetByZincId(string zincId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets molecule image URL.
    /// </summary>
    Task<string?> GetMoleculeImageUrl(string zincId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the API is available.
    /// </summary>
    Task<bool> IsAvailable(CancellationToken cancellationToken = default);
}
