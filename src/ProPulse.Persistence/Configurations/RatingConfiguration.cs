using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProPulse.Persistence.Models;

namespace ProPulse.Persistence.Configurations;

public class RatingConfiguration : BaseEntityTypeConfiguration<Rating>
{
    public override void Configure(EntityTypeBuilder<Rating> builder)
    {
        base.Configure(builder);

        builder.ToTable("Ratings");

        // Configure properties
        builder.Property(e => e.Value)
            .IsRequired()
            .HasAnnotation("Check_Ratings_Value", "Value BETWEEN 1 AND 5");

        // Configure relationships
        builder.HasOne(e => e.Article)
            .WithMany(e => e.Ratings)
            .HasForeignKey(e => e.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(e => e.CreatedBy).HasDatabaseName("IDX_Ratings_CreatedBy");
        builder.HasIndex(e => e.UpdatedBy).HasDatabaseName("IDX_Ratings_UpdatedBy");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IDX_Ratings_CreatedAt");
        builder.HasIndex(e => e.UpdatedAt).HasDatabaseName("IDX_Ratings_UpdatedAt");
        builder.HasIndex(e => e.ArticleId).HasDatabaseName("IDX_Ratings_ArticleId");
    }
}