using Microsoft.EntityFrameworkCore;
using ProPulse.Persistence.Models;
using System.Reflection;

namespace ProPulse.Persistence;

/// <summary>
/// Entity Framework Core DbContext for the ProPulse database
/// </summary>
public class ProPulseDbContext(DbContextOptions<ProPulseDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Rating> Ratings => Set<Rating>();
    
    /// <summary>
    /// Configure the model with all the entity configurations
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}