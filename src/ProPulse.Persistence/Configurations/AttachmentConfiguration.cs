using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProPulse.Persistence.Models;

namespace ProPulse.Persistence.Configurations;

public class AttachmentConfiguration : BaseEntityTypeConfiguration<Attachment>
{
    public override void Configure(EntityTypeBuilder<Attachment> builder)
    {
        base.Configure(builder);

        builder.ToTable("Attachments");

        // Configure properties
        builder.Property(e => e.ContentType)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Locator)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Path)
            .IsRequired()
            .HasMaxLength(255);

        // Configure relationships
        builder.HasOne(e => e.Article)
            .WithMany(e => e.Attachments)
            .HasForeignKey(e => e.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(e => e.ContentType).HasDatabaseName("IX_Attachments_ContentType");
        builder.HasIndex(e => e.Path).HasDatabaseName("IX_Attachments_Path");
        builder.HasIndex(e => e.Locator).HasDatabaseName("IX_Attachments_Locator");
        builder.HasIndex(e => e.CreatedBy).HasDatabaseName("IX_Attachments_CreatedBy");
        builder.HasIndex(e => e.UpdatedBy).HasDatabaseName("IX_Attachments_UpdatedBy");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IDX_Attachments_CreatedAt");
        builder.HasIndex(e => e.UpdatedAt).HasDatabaseName("IDX_Attachments_UpdatedAt");
        builder.HasIndex(e => e.ArticleId).HasDatabaseName("IDX_Attachments_ArticleId");
    }
}