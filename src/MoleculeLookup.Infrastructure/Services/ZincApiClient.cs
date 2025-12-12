using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MoleculeLookup.Core.Enums;
using MoleculeLookup.Core.Interfaces;
using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Infrastructure.Services;

/// <summary>
/// HTTP client for querying the ZINC20 database API.
/// Handles all communication with zinc.docking.org endpoints.
/// </summary>
public class ZincApiClient : IZincApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string BaseUrl = "https://zinc.docking.org";

    public ZincApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    /// <summary>
    /// Searches for a molecule by its SMILES string.
    /// </summary>
    public async Task<SearchResult> SearchBySmiles(string smiles, CancellationToken cancellationToken = default)
    {
        var result = new SearchResult
        {
            QuerySmiles = smiles,
            Status = SearchStatus.InProgress,
            SearchedAt = DateTime.UtcNow
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // URL encode the SMILES string (critical for special characters like =, #, @)
            var encodedSmiles = Uri.EscapeDataString(smiles);
            var url = $"/substances/search/?q={encodedSmiles}&output=json";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                result.Status = SearchStatus.Completed;
                result.IsFound = false;
                result.ErrorMessage = "Molecule not found in ZINC20 database";
                return result;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResponse = JsonSerializer.Deserialize<ZincSearchResponse>(content, _jsonOptions);

            if (searchResponse?.Results != null && searchResponse.Results.Count > 0)
            {
                var firstResult = searchResponse.Results[0];
                result.IsFound = true;
                result.MoleculeData = await GetByZincId(firstResult.ZincId, cancellationToken);
            }
            else
            {
                result.IsFound = false;
                result.ErrorMessage = "No matching molecules found";
            }

            result.Status = SearchStatus.Completed;
        }
        catch (HttpRequestException ex)
        {
            result.Status = SearchStatus.Failed;
            result.ErrorMessage = $"API request failed: {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            result.Status = SearchStatus.Cancelled;
            result.ErrorMessage = "Request was cancelled or timed out";
        }
        catch (JsonException ex)
        {
            result.Status = SearchStatus.Failed;
            result.ErrorMessage = $"Failed to parse API response: {ex.Message}";
        }
        finally
        {
            stopwatch.Stop();
            result.SearchDuration = stopwatch.Elapsed;
        }

        return result;
    }

    /// <summary>
    /// Gets full molecule data by ZINC ID.
    /// </summary>
    public async Task<MoleculeData?> GetByZincId(string zincId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Normalize ZINC ID format (e.g., "ZINC000000895" or just "895")
            var normalizedId = NormalizeZincId(zincId);
            var url = $"/substances/{normalizedId}.json";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var zincData = JsonSerializer.Deserialize<ZincSubstanceResponse>(content, _jsonOptions);

            if (zincData == null)
            {
                return null;
            }

            return MapToMoleculeData(zincData);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets molecule image URL.
    /// </summary>
    public Task<string?> GetMoleculeImageUrl(string zincId, CancellationToken cancellationToken = default)
    {
        var normalizedId = NormalizeZincId(zincId);
        var imageUrl = $"{BaseUrl}/substances/{normalizedId}.png";
        return Task.FromResult<string?>(imageUrl);
    }

    /// <summary>
    /// Checks if the ZINC20 API is available.
    /// </summary>
    public async Task<bool> IsAvailable(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Normalizes a ZINC ID to the full format (e.g., "ZINC000000895").
    /// </summary>
    private static string NormalizeZincId(string zincId)
    {
        if (string.IsNullOrWhiteSpace(zincId))
            return zincId;

        // If already in ZINC format, return as-is
        if (zincId.StartsWith("ZINC", StringComparison.OrdinalIgnoreCase))
            return zincId.ToUpper();

        // If numeric, pad to 12 digits and add ZINC prefix
        if (long.TryParse(zincId, out var numericId))
        {
            return $"ZINC{numericId:D12}";
        }

        return zincId;
    }

    /// <summary>
    /// Maps the ZINC API response to our MoleculeData model.
    /// </summary>
    private MoleculeData MapToMoleculeData(ZincSubstanceResponse zinc)
    {
        return new MoleculeData
        {
            ZincId = zinc.ZincId ?? string.Empty,
            SmilesString = zinc.Smiles ?? string.Empty,
            Name = zinc.Name ?? zinc.ZincId ?? "Unknown",
            MolecularWeight = zinc.MolecularWeight,
            MolecularFormula = zinc.MolecularFormula,
            LogP = zinc.LogP,
            HydrogenBondDonors = zinc.NumHydrogenBondDonors,
            HydrogenBondAcceptors = zinc.NumHydrogenBondAcceptors,
            PolarSurfaceArea = zinc.Tpsa,
            RotatableBonds = zinc.NumRotatableBonds,
            RingCount = zinc.NumRings,
            HeavyAtomCount = zinc.NumHeavyAtoms,
            RuleOfFiveCompliant = zinc.RuleOfFive,
            InChI = zinc.InChI,
            InChIKey = zinc.InChIKey,
            ImageUrl = $"{BaseUrl}/substances/{zinc.ZincId}.png",
            RetrievedAt = DateTime.UtcNow,
            DataSource = "ZINC20"
        };
    }
}

#region ZINC API Response Models

/// <summary>
/// Response from ZINC search endpoint.
/// </summary>
internal class ZincSearchResponse
{
    public List<ZincSearchResult> Results { get; set; } = new();
    public int Count { get; set; }
}

/// <summary>
/// Individual search result from ZINC.
/// </summary>
internal class ZincSearchResult
{
    public string ZincId { get; set; } = string.Empty;
    public string Smiles { get; set; } = string.Empty;
    public string? Name { get; set; }
}

/// <summary>
/// Full substance data from ZINC API.
/// </summary>
internal class ZincSubstanceResponse
{
    public string? ZincId { get; set; }
    public string? Smiles { get; set; }
    public string? Name { get; set; }
    public double? MolecularWeight { get; set; }
    public string? MolecularFormula { get; set; }
    public double? LogP { get; set; }
    public int? NumHydrogenBondDonors { get; set; }
    public int? NumHydrogenBondAcceptors { get; set; }
    public double? Tpsa { get; set; } // Topological Polar Surface Area
    public int? NumRotatableBonds { get; set; }
    public int? NumRings { get; set; }
    public int? NumHeavyAtoms { get; set; }
    public bool? RuleOfFive { get; set; }
    public string? InChI { get; set; }
    public string? InChIKey { get; set; }
}

#endregion
