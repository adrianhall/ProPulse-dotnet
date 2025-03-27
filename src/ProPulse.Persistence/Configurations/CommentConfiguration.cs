using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProPulse.Persistence.Models;

namespace ProPulse.Persistence.Configurations;

public class CommentConfiguration : BaseEntityTypeConfiguration<Comment>
{
    public override void Configure(EntityTypeBuilder<Comment> builder)
    {
        base.Configure(builder);

        builder.ToTable("Comments");

        // Configure properties
        builder.Property(e => e.Content)
            .IsRequired()
            .HasColumnType("TEXT");

        // Configure relationships
        builder.HasOne(e => e.Article)
            .WithMany(e => e.Comments)
            .HasForeignKey(e => e.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(e => e.CreatedBy).HasDatabaseName("IDX_Comments_CreatedBy");
        builder.HasIndex(e => e.UpdatedBy).HasDatabaseName("IDX_Comments_UpdatedBy");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IDX_Comments_CreatedAt");
        builder.HasIndex(e => e.UpdatedAt).HasDatabaseName("IDX_Comments_UpdatedAt");
        builder.HasIndex(e => e.ArticleId).HasDatabaseName("IDX_Comments_ArticleId");
    }
}