using FluentAssertions;
using MoleculeLookup.Core.Models;
using MoleculeLookup.Infrastructure.Services;
using Xunit;

namespace MoleculeLookup.Tests.Unit;

/// <summary>
/// Unit tests for the SMILES converter.
/// </summary>
public class SmilesConverterTests
{
    private readonly SmilesConverter _converter;

    public SmilesConverterTests()
    {
        _converter = new SmilesConverter();
    }

    #region ToSmiles Tests

    [Fact]
    public void ToSmiles_EmptyMolecule_ReturnsEmptyString()
    {
        // Arrange
        var molecule = new DrawnMolecule();

        // Act
        var smiles = _converter.ToSmiles(molecule);

        // Assert
        smiles.Should().BeEmpty();
    }

    [Fact]
    public void ToSmiles_SingleAtom_ReturnsAtomSymbol()
    {
        // Arrange
        var molecule = new DrawnMolecule
        {
            Atoms = new List<Atom>
            {
                new Atom { Id = 0, Symbol = "C", ImplicitHydrogens = 4 }
            }
        };

        // Act
        var smiles = _converter.ToSmiles(molecule);

        // Assert
        smiles.Should().NotBeEmpty();
        smiles.Should().Contain("C");
    }

    [Fact]
    public void ToSmiles_LinearMolecule_ReturnsValidSmiles()
    {
        // Arrange - Ethane (C-C)
        var molecule = new DrawnMolecule
        {
            Atoms = new List<Atom>
            {
                new Atom { Id = 0, Symbol = "C", ImplicitHydrogens = 3 },
                new Atom { Id = 1, Symbol = "C", ImplicitHydrogens = 3 }
            },
            Bonds = new List<Bond>
            {
                new Bond { Id = 0, Atom1Id = 0, Atom2Id = 1, Type = BondType.Single }
            }
        };

        // Act
        var smiles = _converter.ToSmiles(molecule);

        // Assert
        smiles.Should().Contain("C");
        smiles.Length.Should().BeGreaterThan(1);
    }

    [Fact]
    public void ToSmiles_DoubleBond_IncludesDoubleBondSymbol()
    {
        // Arrange - Ethene (C=C)
        var molecule = new DrawnMolecule
        {
            Atoms = new List<Atom>
            {
                new Atom { Id = 0, Symbol = "C", ImplicitHydrogens = 2 },
                new Atom { Id = 1, Symbol = "C", ImplicitHydrogens = 2 }
            },
            Bonds = new List<Bond>
            {
                new Bond { Id = 0, Atom1Id = 0, Atom2Id = 1, Type = BondType.Double }
            }
        };

        // Act
        var smiles = _converter.ToSmiles(molecule);

        // Assert
        smiles.Should().Contain("=");
    }

    [Fact]
    public void ToSmiles_ChargedAtom_IncludesChargeInBrackets()
    {
        // Arrange - Ammonium (NH4+)
        var molecule = new DrawnMolecule
        {
            Atoms = new List<Atom>
            {
                new Atom { Id = 0, Symbol = "N", FormalCharge = 1, ImplicitHydrogens = 4 }
            }
        };

        // Act
        var smiles = _converter.ToSmiles(molecule);

        // Assert
        smiles.Should().Contain("[");
        smiles.Should().Contain("]");
        smiles.Should().Contain("+");
    }

    [Fact]
    public void ToSmiles_ChiralCenter_IncludesChirality()
    {
        // Arrange
        var molecule = new DrawnMolecule
        {
            Atoms = new List<Atom>
            {
                new Atom { Id = 0, Symbol = "C", IsChiralCenter = true, ChiralConfiguration = "S" }
            }
        };

        // Act
        var smiles = _converter.ToSmiles(molecule);

        // Assert
        smiles.Should().Contain("@");
    }

    #endregion

    #region FromSmiles Tests

    [Fact]
    public void FromSmiles_EmptyString_ReturnsEmptyMolecule()
    {
        // Act
        var molecule = _converter.FromSmiles("");

        // Assert
        molecule.Atoms.Should().BeEmpty();
        molecule.Bonds.Should().BeEmpty();
    }

    [Fact]
    public void FromSmiles_SimpleSmiles_ParsesCorrectly()
    {
        // Arrange
        var smiles = "CCO"; // Ethanol

        // Act
        var molecule = _converter.FromSmiles(smiles);

        // Assert
        molecule.Atoms.Should().HaveCount(3);
        molecule.Bonds.Should().HaveCount(2);
    }

    [Fact]
    public void FromSmiles_WithDoubleBond_ParsesBondType()
    {
        // Arrange
        var smiles = "C=O"; // Formaldehyde

        // Act
        var molecule = _converter.FromSmiles(smiles);

        // Assert
        molecule.Atoms.Should().HaveCount(2);
        molecule.Bonds.Should().ContainSingle(b => b.Type == BondType.Double);
    }

    [Fact]
    public void FromSmiles_WithBranch_ParsesCorrectly()
    {
        // Arrange
        var smiles = "CC(C)C"; // Isobutane

        // Act
        var molecule = _converter.FromSmiles(smiles);

        // Assert
        molecule.Atoms.Should().HaveCount(4);
        molecule.Bonds.Should().HaveCount(3);
    }

    [Fact]
    public void FromSmiles_WithRing_ParsesRingClosure()
    {
        // Arrange
        var smiles = "C1CCCCC1"; // Cyclohexane

        // Act
        var molecule = _converter.FromSmiles(smiles);

        // Assert
        molecule.Atoms.Should().HaveCount(6);
        molecule.Bonds.Should().HaveCount(6); // Including ring closure
    }

    [Fact]
    public void FromSmiles_BracketedAtom_ParsesCharge()
    {
        // Arrange
        var smiles = "[NH4+]";

        // Act
        var molecule = _converter.FromSmiles(smiles);

        // Assert
        molecule.Atoms.Should().ContainSingle();
        molecule.Atoms[0].Symbol.Should().Be("N");
        molecule.Atoms[0].FormalCharge.Should().Be(1);
    }

    [Fact]
    public void FromSmiles_TwoLetterElement_ParsesCorrectly()
    {
        // Arrange
        var smiles = "CCl"; // Chloromethane

        // Act
        var molecule = _converter.FromSmiles(smiles);

        // Assert
        molecule.Atoms.Should().HaveCount(2);
        molecule.Atoms.Should().Contain(a => a.Symbol == "Cl");
    }

    #endregion

    #region IsValidSmiles Tests

    [Theory]
    [InlineData("CCO", true)]
    [InlineData("c1ccccc1", true)]
    [InlineData("CC(=O)O", true)]
    [InlineData("[NH4+]", true)]
    [InlineData("", false)]
    [InlineData("XYZ", true)] // Valid syntax, invalid chemistry
    [InlineData("C(C", false)] // Unbalanced parentheses
    [InlineData("[CH4", false)] // Unbalanced brackets
    public void IsValidSmiles_VariousInputs_ReturnsExpected(string smiles, bool expected)
    {
        // Act
        var result = _converter.IsValidSmiles(smiles);

        // Assert
        result.Should().Be(expected);
    }

    #endregion
}
