using FluentAssertions;
using MoleculeLookup.Core.Enums;
using MoleculeLookup.Core.Models;
using MoleculeLookup.Core.Patterns.ChainOfResponsibility;
using MoleculeLookup.Core.Validation;
using Xunit;

namespace MoleculeLookup.Tests.Unit;

/// <summary>
/// Unit tests for the Chain of Responsibility validation pattern.
/// </summary>
public class ValidationHandlerTests
{
    #region ChargeBalanceHandler Tests

    [Fact]
    public void ChargeBalanceHandler_ValidNeutralMolecule_ReturnsSuccess()
    {
        // Arrange
        var handler = new ChargeBalanceHandler();
        var molecule = CreateWaterMolecule(); // H2O is neutral

        // Act
        var result = handler.Handle(molecule);

        // Assert
        result.IsValid.Should().BeTrue();
        result.FailureReason.Should().Be(ValidationFailureReason.None);
    }

    [Fact]
    public void ChargeBalanceHandler_EmptyMolecule_ReturnsFailure()
    {
        // Arrange
        var handler = new ChargeBalanceHandler();
        var molecule = new DrawnMolecule();

        // Act
        var result = handler.Handle(molecule);

        // Assert
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be(ValidationFailureReason.EmptyStructure);
    }

    [Fact]
    public void ChargeBalanceHandler_ExcessiveCharge_ReturnsFailure()
    {
        // Arrange
        var handler = new ChargeBalanceHandler();
        var molecule = new DrawnMolecule
        {
            Atoms = new List<Atom>
            {
                new Atom { Id = 0, Symbol = "C", FormalCharge = 5 } // Unrealistic charge
            }
        };

        // Act
        var result = handler.Handle(molecule);

        // Assert
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be(ValidationFailureReason.ChargeImbalance);
    }

    #endregion

    #region BondingRulesHandler Tests

    [Fact]
    public void BondingRulesHandler_ValidMethane_ReturnsSuccess()
    {
        // Arrange
        var handler = new BondingRulesHandler();
        var molecule = CreateMethaneMolecule(); // CH4

        // Act
        var result = handler.Handle(molecule);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void BondingRulesHandler_OvervalentCarbon_ReturnsFailure()
    {
        // Arrange
        var handler = new BondingRulesHandler();
        var molecule = new DrawnMolecule
        {
            Atoms = new List<Atom>
            {
                new Atom { Id = 0, Symbol = "C", ImplicitHydrogens = 0 },
                new Atom { Id = 1, Symbol = "H" },
                new Atom { Id = 2, Symbol = "H" },
                new Atom { Id = 3, Symbol = "H" },
                new Atom { Id = 4, Symbol = "H" },
                new Atom { Id = 5, Symbol = "H" } // 5 H's would be overvalent
            },
            Bonds = new List<Bond>
            {
                new Bond { Id = 0, Atom1Id = 0, Atom2Id = 1, Type = BondType.Single },
                new Bond { Id = 1, Atom1Id = 0, Atom2Id = 2, Type = BondType.Single },
                new Bond { Id = 2, Atom1Id = 0, Atom2Id = 3, Type = BondType.Single },
                new Bond { Id = 3, Atom1Id = 0, Atom2Id = 4, Type = BondType.Single },
                new Bond { Id = 4, Atom1Id = 0, Atom2Id = 5, Type = BondType.Single }
            }
        };

        // Act
        var result = handler.Handle(molecule);

        // Assert
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be(ValidationFailureReason.InvalidBondingRules);
    }

    #endregion

    #region StereochemistryHandler Tests

    [Fact]
    public void StereochemistryHandler_ValidChiralCenter_ReturnsSuccess()
    {
        // Arrange
        var handler = new StereochemistryHandler();
        var molecule = CreateAlanineMolecule(); // Has a chiral center

        // Act
        var result = handler.Handle(molecule);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void StereochemistryHandler_InvalidChiralConfiguration_ReturnsFailure()
    {
        // Arrange
        var handler = new StereochemistryHandler();
        var molecule = new DrawnMolecule
        {
            Atoms = new List<Atom>
            {
                new Atom
                {
                    Id = 0,
                    Symbol = "C",
                    IsChiralCenter = true,
                    ChiralConfiguration = "X" // Invalid - should be R or S
                }
            }
        };

        // Act
        var result = handler.Handle(molecule);

        // Assert
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be(ValidationFailureReason.InvalidStereochemistry);
    }

    #endregion

    #region Full Chain Tests

    [Fact]
    public void MoleculeValidator_ValidMolecule_PassesAllHandlers()
    {
        // Arrange
        var validator = MoleculeValidator.CreateDefault();
        var molecule = CreateEthanolMolecule(); // C2H5OH

        // Act
        var result = validator.Validate(molecule);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void MoleculeValidator_NullMolecule_ReturnsFailure()
    {
        // Arrange
        var validator = MoleculeValidator.CreateDefault();

        // Act
        var result = validator.Validate(null!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be(ValidationFailureReason.EmptyStructure);
    }

    [Fact]
    public void MoleculeValidator_ChainStopsOnFirstFailure()
    {
        // Arrange - Create validator with custom chain
        var validator = MoleculeValidator.CreateDefault();

        // Empty molecule should fail at charge handler (first in chain)
        var molecule = new DrawnMolecule();

        // Act
        var result = validator.Validate(molecule);

        // Assert
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be(ValidationFailureReason.EmptyStructure);
    }

    #endregion

    #region Helper Methods

    private DrawnMolecule CreateWaterMolecule()
    {
        return new DrawnMolecule
        {
            Atoms = new List<Atom>
            {
                new Atom { Id = 0, Symbol = "O", ImplicitHydrogens = 2 }
            }
        };
    }

    private DrawnMolecule CreateMethaneMolecule()
    {
        return new DrawnMolecule
        {
            Atoms = new List<Atom>
            {
                new Atom { Id = 0, Symbol = "C", ImplicitHydrogens = 4 }
            }
        };
    }

    private DrawnMolecule CreateEthanolMolecule()
    {
        return new DrawnMolecule
        {
            Atoms = new List<Atom>
            {
                new Atom { Id = 0, Symbol = "C", ImplicitHydrogens = 3 },
                new Atom { Id = 1, Symbol = "C", ImplicitHydrogens = 2 },
                new Atom { Id = 2, Symbol = "O", ImplicitHydrogens = 1 }
            },
            Bonds = new List<Bond>
            {
                new Bond { Id = 0, Atom1Id = 0, Atom2Id = 1, Type = BondType.Single },
                new Bond { Id = 1, Atom1Id = 1, Atom2Id = 2, Type = BondType.Single }
            }
        };
    }

    private DrawnMolecule CreateAlanineMolecule()
    {
        // Simplified L-Alanine structure
        return new DrawnMolecule
        {
            Atoms = new List<Atom>
            {
                new Atom { Id = 0, Symbol = "C", ImplicitHydrogens = 3 }, // Methyl
                new Atom { Id = 1, Symbol = "C", ImplicitHydrogens = 1, IsChiralCenter = true, ChiralConfiguration = "S" }, // Chiral center
                new Atom { Id = 2, Symbol = "N", ImplicitHydrogens = 2 }, // Amine
                new Atom { Id = 3, Symbol = "C", ImplicitHydrogens = 0 }, // Carboxyl C
                new Atom { Id = 4, Symbol = "O", ImplicitHydrogens = 0 }, // Carbonyl O
                new Atom { Id = 5, Symbol = "O", ImplicitHydrogens = 1 }  // Hydroxyl O
            },
            Bonds = new List<Bond>
            {
                new Bond { Id = 0, Atom1Id = 0, Atom2Id = 1, Type = BondType.Single },
                new Bond { Id = 1, Atom1Id = 1, Atom2Id = 2, Type = BondType.Single },
                new Bond { Id = 2, Atom1Id = 1, Atom2Id = 3, Type = BondType.Single },
                new Bond { Id = 3, Atom1Id = 3, Atom2Id = 4, Type = BondType.Double },
                new Bond { Id = 4, Atom1Id = 3, Atom2Id = 5, Type = BondType.Single }
            }
        };
    }

    #endregion
}
