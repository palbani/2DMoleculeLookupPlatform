namespace MoleculeLookup.Core.Models;

/// <summary>
/// Comprehensive chemical data for a molecule retrieved from ZINC20 database.
/// Contains all properties returned by the API.
/// </summary>
public class MoleculeData
{
    public string ZincId { get; set; } = string.Empty;
    public string SmilesString { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // Physical properties
    public double? MolecularWeight { get; set; }
    public string? MolecularFormula { get; set; }
    public double? LogP { get; set; } // Partition coefficient
    public int? HydrogenBondDonors { get; set; }
    public int? HydrogenBondAcceptors { get; set; }
    public double? PolarSurfaceArea { get; set; }
    public int? RotatableBonds { get; set; }
    public int? RingCount { get; set; }
    public int? HeavyAtomCount { get; set; }

    // Lipinski's Rule of Five compliance
    public bool? RuleOfFiveCompliant { get; set; }

    // Vendor/availability information
    public List<VendorInfo> Vendors { get; set; } = new();

    // 2D/3D structure representations
    public string? InChI { get; set; }
    public string? InChIKey { get; set; }
    public string? Mol2D { get; set; }
    public string? Mol3D { get; set; }
    public string? ImageUrl { get; set; }

    // Metadata
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
    public string? DataSource { get; set; } = "ZINC20";
}

/// <summary>
/// Information about a chemical vendor/supplier.
/// </summary>
public class VendorInfo
{
    public string VendorName { get; set; } = string.Empty;
    public string? CatalogNumber { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public string? Availability { get; set; }
    public string? Url { get; set; }
}
