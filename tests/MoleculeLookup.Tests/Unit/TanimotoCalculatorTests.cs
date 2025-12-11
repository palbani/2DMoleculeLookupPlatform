using FluentAssertions;
using MoleculeLookup.Core.Models;
using MoleculeLookup.Infrastructure.Services;
using Xunit;

namespace MoleculeLookup.Tests.Unit;

/// <summary>
/// Unit tests for the Tanimoto coefficient calculator.
/// </summary>
public class TanimotoCalculatorTests
{
    private readonly TanimotoCalculator _calculator;

    public TanimotoCalculatorTests()
    {
        _calculator = new TanimotoCalculator();
    }

    [Fact]
    public void Calculate_IdenticalMolecules_ReturnsOne()
    {
        // Arrange
        var smiles = "CCO"; // Ethanol

        // Act
        var result = _calculator.Calculate(smiles, smiles);

        // Assert
        result.Should().Be(1.0);
    }

    [Fact]
    public void Calculate_CompletelyDifferentMolecules_ReturnsLowValue()
    {
        // Arrange
        var smiles1 = "C";      // Methane
        var smiles2 = "c1ccccc1"; // Benzene

        // Act
        var result = _calculator.Calculate(smiles1, smiles2);

        // Assert
        result.Should().BeLessThan(0.5);
    }

    [Fact]
    public void Calculate_SimilarMolecules_ReturnsHighValue()
    {
        // Arrange
        var smiles1 = "CCO";    // Ethanol
        var smiles2 = "CCCO";   // Propanol

        // Act
        var result = _calculator.Calculate(smiles1, smiles2);

        // Assert
        result.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void Calculate_EmptySmiles_ReturnsZero()
    {
        // Arrange & Act
        var result = _calculator.Calculate("", "CCO");

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void Calculate_BothEmpty_ReturnsOne()
    {
        // Arrange
        var fp1 = new HashSet<int>();
        var fp2 = new HashSet<int>();

        // Act
        var result = _calculator.Calculate(fp1, fp2);

        // Assert
        result.Should().Be(1.0);
    }

    [Fact]
    public void GenerateFingerprint_ValidSmiles_ReturnsNonEmptySet()
    {
        // Arrange
        var smiles = "CC(=O)O"; // Acetic acid

        // Act
        var fingerprint = _calculator.GenerateFingerprint(smiles);

        // Assert
        fingerprint.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateFingerprint_EmptySmiles_ReturnsEmptySet()
    {
        // Act
        var fingerprint = _calculator.GenerateFingerprint("");

        // Assert
        fingerprint.Should().BeEmpty();
    }

    [Fact]
    public void FindSimilar_WithThreshold_ReturnsMatchingMolecules()
    {
        // Arrange
        var querySmiles = "CCO"; // Ethanol
        var candidates = new List<MoleculeMetadata>
        {
            new MoleculeMetadata { ZincId = "Z1", SmilesString = "CCCO", Name = "Propanol" },
            new MoleculeMetadata { ZincId = "Z2", SmilesString = "CCO", Name = "Ethanol" },
            new MoleculeMetadata { ZincId = "Z3", SmilesString = "c1ccccc1", Name = "Benzene" },
            new MoleculeMetadata { ZincId = "Z4", SmilesString = "CCCCO", Name = "Butanol" }
        };

        // Act
        var results = _calculator.FindSimilar(querySmiles, candidates, 0.5).ToList();

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.Metadata.Name == "Ethanol");
        results.First().TanimotoCoefficient.Should().Be(1.0); // Exact match
    }

    [Fact]
    public void FindSimilar_HighThreshold_ReturnsFewerResults()
    {
        // Arrange
        var querySmiles = "CCO";
        var candidates = new List<MoleculeMetadata>
        {
            new MoleculeMetadata { ZincId = "Z1", SmilesString = "CCCO", Name = "Propanol" },
            new MoleculeMetadata { ZincId = "Z2", SmilesString = "CCO", Name = "Ethanol" },
            new MoleculeMetadata { ZincId = "Z3", SmilesString = "CCCCCO", Name = "Pentanol" }
        };

        // Act
        var highThresholdResults = _calculator.FindSimilar(querySmiles, candidates, 0.95).ToList();
        var lowThresholdResults = _calculator.FindSimilar(querySmiles, candidates, 0.3).ToList();

        // Assert
        highThresholdResults.Count.Should().BeLessThanOrEqualTo(lowThresholdResults.Count);
    }

    [Fact]
    public void SerializeFingerprint_RoundTrip_PreservesData()
    {
        // Arrange
        var originalFingerprint = new HashSet<int> { 1, 5, 10, 100, 500 };

        // Act
        var serialized = TanimotoCalculator.SerializeFingerprint(originalFingerprint);
        var deserialized = TanimotoCalculator.ParseFingerprintBits(serialized);

        // Assert
        deserialized.Should().BeEquivalentTo(originalFingerprint);
    }

    [Fact]
    public void Calculate_UsingPrecomputedFingerprints_MatchesDirectCalculation()
    {
        // Arrange
        var smiles1 = "CCO";
        var smiles2 = "CCCO";

        var fp1 = _calculator.GenerateFingerprint(smiles1);
        var fp2 = _calculator.GenerateFingerprint(smiles2);

        // Act
        var directResult = _calculator.Calculate(smiles1, smiles2);
        var fingerprintResult = _calculator.Calculate(fp1, fp2);

        // Assert
        fingerprintResult.Should().Be(directResult);
    }
}
