namespace ProPulse.Persistence.Models;

/// <summary>
/// Represents an article in the system
/// </summary>
public class Article : BaseEntity
{
    /// <summary>
    /// Article's content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the article was published
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Date and time until which the article is published
    /// </summary>
    public DateTime? PublishedUntil { get; set; }

    /// <summary>
    /// Current state of the article (Draft, Published, Retired)
    /// </summary>
    public ArticleState State { get; set; } = ArticleState.Draft;

    /// <summary>
    /// Short summary of the article
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Title of the article
    /// </summary>
    public string Title { get; set; } = string.Empty;

    // Navigation properties
    /// <summary>
    /// Comments associated with this article
    /// </summary>
    public virtual ICollection<Comment> Comments { get; set; } = [];

    /// <summary>
    /// Tags associated with this article
    /// </summary>
    public virtual ICollection<Tag> Tags { get; set; } = [];

    /// <summary>
    /// Attachments associated with this article
    /// </summary>
    public virtual ICollection<Attachment> Attachments { get; set; } = [];

    /// <summary>
    /// Ratings associated with this article
    /// </summary>
    public virtual ICollection<Rating> Ratings { get; set; } = [];
}