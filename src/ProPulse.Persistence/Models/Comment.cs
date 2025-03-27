namespace ProPulse.Persistence.Models;

/// <summary>
/// Represents a comment on an article
/// </summary>
public class Comment : BaseEntity
{
    /// <summary>
    /// Comment content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// ID of the article this comment belongs to
    /// </summary>
    public Guid ArticleId { get; set; }

    /// <summary>
    /// Navigation property to the associated article
    /// </summary>
    public virtual Article? Article { get; set; }
}