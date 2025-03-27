using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProPulse.Persistence.Models;

namespace ProPulse.Persistence.Configurations;

/// <summary>
/// Base configuration class for all entities
/// </summary>
public abstract class BaseEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        // Configure primary key
        builder.HasKey(e => e.Id);

        // Configure base properties
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(64);

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(64);

        builder.Property(e => e.Version)
            .IsRowVersion();
    }
}