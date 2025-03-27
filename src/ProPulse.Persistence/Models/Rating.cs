namespace ProPulse.Persistence.Models;

/// <summary>
/// Represents a user rating for an article
/// </summary>
public class Rating : BaseEntity
{
    /// <summary>
    /// Rating value (1-5)
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// ID of the article this rating belongs to
    /// </summary>
    public Guid ArticleId { get; set; }

    /// <summary>
    /// Navigation property to the associated article
    /// </summary>
    public virtual Article? Article { get; set; }
}