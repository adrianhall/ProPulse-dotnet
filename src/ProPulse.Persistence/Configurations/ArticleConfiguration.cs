using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProPulse.Persistence.Models;

namespace ProPulse.Persistence.Configurations;

public class ArticleConfiguration : BaseEntityTypeConfiguration<Article>
{
    public override void Configure(EntityTypeBuilder<Article> builder)
    {
        base.Configure(builder);

        builder.ToTable("Articles");

        // Configure properties
        builder.Property(e => e.Content)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(e => e.PublishedAt)
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(e => e.PublishedUntil)
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(e => e.State)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>()
            .HasDefaultValue(ArticleState.Draft);

        builder.Property(e => e.Summary)
            .HasMaxLength(4096);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(255);

        // Configure relationships
        builder.HasMany(e => e.Comments)
            .WithOne(e => e.Article)
            .HasForeignKey(e => e.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Ratings)
            .WithOne(e => e.Article)
            .HasForeignKey(e => e.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Attachments)
            .WithOne(e => e.Article)
            .HasForeignKey(e => e.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure many-to-many relationship with Tags
        builder.HasMany(e => e.Tags)
            .WithMany(e => e.Articles)
            .UsingEntity(
                "ArticleTags",
                l => l.HasOne(typeof(Tag)).WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade),
                r => r.HasOne(typeof(Article)).WithMany().HasForeignKey("ArticleId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasKey("ArticleId", "TagId"));

        // Configure indexes
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IDX_Articles_CreatedAt");
        builder.HasIndex(e => e.CreatedBy).HasDatabaseName("IDX_Articles_CreatedBy");
        builder.HasIndex(e => e.PublishedAt).HasDatabaseName("IDX_Articles_PublishedAt");
        builder.HasIndex(e => e.PublishedUntil).HasDatabaseName("IDX_Articles_PublishedUntil");
        builder.HasIndex(e => e.State).HasDatabaseName("IDX_Articles_State");
        builder.HasIndex(e => e.UpdatedAt).HasDatabaseName("IDX_Articles_UpdatedAt");
        builder.HasIndex(e => e.UpdatedBy).HasDatabaseName("IDX_Articles_UpdatedBy");
    }
}