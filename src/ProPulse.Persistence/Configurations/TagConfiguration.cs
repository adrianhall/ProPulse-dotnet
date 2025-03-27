using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProPulse.Persistence.Models;

namespace ProPulse.Persistence.Configurations;

public class TagConfiguration : BaseEntityTypeConfiguration<Tag>
{
    public override void Configure(EntityTypeBuilder<Tag> builder)
    {
        base.Configure(builder);

        builder.ToTable("Tags");

        // Configure properties
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.NormalizedName)
            .IsRequired()
            .HasMaxLength(255);

        // Configure indexes
        builder.HasIndex(e => e.Name).HasDatabaseName("IDX_Tags_Name");
        builder.HasIndex(e => e.NormalizedName).HasDatabaseName("IDX_Tags_NormalizedName");
        builder.HasIndex(e => e.CreatedBy).HasDatabaseName("IDX_Tags_CreatedBy");
        builder.HasIndex(e => e.UpdatedBy).HasDatabaseName("IDX_Tags_UpdatedBy");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IDX_Tags_CreatedAt");
        builder.HasIndex(e => e.UpdatedAt).HasDatabaseName("IDX_Tags_UpdatedAt");
    }
}