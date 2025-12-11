using Microsoft.EntityFrameworkCore;
using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Infrastructure.Database;

/// <summary>
/// Entity Framework Core database context for the molecule lookup platform.
/// Stores molecule metadata for premium similarity search and search history.
/// </summary>
public class MoleculeDbContext : DbContext
{
    public MoleculeDbContext(DbContextOptions<MoleculeDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Molecule metadata for similarity search (premium feature).
    /// </summary>
    public DbSet<MoleculeMetadataEntity> MoleculeMetadata { get; set; } = null!;

    /// <summary>
    /// Search history entries.
    /// </summary>
    public DbSet<SearchHistoryEntity> SearchHistory { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MoleculeMetadata configuration
        modelBuilder.Entity<MoleculeMetadataEntity>(entity =>
        {
            entity.ToTable("MoleculeMetadata");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ZincId).IsUnique();
            entity.HasIndex(e => e.SmilesString);

            entity.Property(e => e.ZincId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SmilesString).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.FingerprintBits).HasMaxLength(10000);
        });

        // SearchHistory configuration
        modelBuilder.Entity<SearchHistoryEntity>(entity =>
        {
            entity.ToTable("SearchHistory");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsFavorite);

            entity.Property(e => e.SmilesString).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.MoleculeName).HasMaxLength(500);
            entity.Property(e => e.ZincId).HasMaxLength(50);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.Tags).HasMaxLength(500);
        });
    }
}

/// <summary>
/// Database entity for molecule metadata.
/// </summary>
public class MoleculeMetadataEntity
{
    public int Id { get; set; }
    public string ZincId { get; set; } = string.Empty;
    public string SmilesString { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string FingerprintBits { get; set; } = string.Empty;
    public int FingerprintBitCount { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public MoleculeMetadata ToModel()
    {
        return new MoleculeMetadata
        {
            Id = Id,
            ZincId = ZincId,
            SmilesString = SmilesString,
            Name = Name,
            ImageUrl = ImageUrl,
            FingerprintBits = FingerprintBits,
            FingerprintBitCount = FingerprintBitCount,
            AddedAt = AddedAt
        };
    }

    public static MoleculeMetadataEntity FromModel(MoleculeMetadata model)
    {
        return new MoleculeMetadataEntity
        {
            Id = model.Id,
            ZincId = model.ZincId,
            SmilesString = model.SmilesString,
            Name = model.Name,
            ImageUrl = model.ImageUrl,
            FingerprintBits = model.FingerprintBits,
            FingerprintBitCount = model.FingerprintBitCount,
            AddedAt = model.AddedAt
        };
    }
}

/// <summary>
/// Database entity for search history.
/// </summary>
public class SearchHistoryEntity
{
    public Guid Id { get; set; }
    public string SmilesString { get; set; } = string.Empty;
    public string? MoleculeName { get; set; }
    public string? ZincId { get; set; }
    public string? ImageUrl { get; set; }
    public byte[]? ThumbnailImage { get; set; }
    public int SearchType { get; set; }
    public double? SimilarityThreshold { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }
    public bool IsFavorite { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }

    public SearchHistoryEntry ToModel()
    {
        return new SearchHistoryEntry
        {
            Id = Id,
            SmilesString = SmilesString,
            MoleculeName = MoleculeName,
            ZincId = ZincId,
            ImageUrl = ImageUrl,
            ThumbnailImage = ThumbnailImage,
            SearchType = (SearchType)SearchType,
            SimilarityThreshold = SimilarityThreshold,
            Status = (Core.Enums.SearchStatus)Status,
            CreatedAt = CreatedAt,
            LastAccessedAt = LastAccessedAt,
            IsFavorite = IsFavorite,
            Notes = Notes,
            Tags = Tags
        };
    }

    public static SearchHistoryEntity FromModel(SearchHistoryEntry model)
    {
        return new SearchHistoryEntity
        {
            Id = model.Id,
            SmilesString = model.SmilesString,
            MoleculeName = model.MoleculeName,
            ZincId = model.ZincId,
            ImageUrl = model.ImageUrl,
            ThumbnailImage = model.ThumbnailImage,
            SearchType = (int)model.SearchType,
            SimilarityThreshold = model.SimilarityThreshold,
            Status = (int)model.Status,
            CreatedAt = model.CreatedAt,
            LastAccessedAt = model.LastAccessedAt,
            IsFavorite = model.IsFavorite,
            Notes = model.Notes,
            Tags = model.Tags
        };
    }
}
