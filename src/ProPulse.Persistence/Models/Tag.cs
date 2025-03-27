namespace ProPulse.Persistence.Models;

/// <summary>
/// Represents a tag for categorizing articles
/// </summary>
public class Tag : BaseEntity
{
    /// <summary>
    /// Name of the tag
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Normalized name of the tag (for searching/indexing)
    /// </summary>
    public string NormalizedName { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to associated articles
    /// </summary>
    public virtual ICollection<Article> Articles { get; set; } = [];
}