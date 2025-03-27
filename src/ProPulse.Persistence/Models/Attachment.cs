namespace ProPulse.Persistence.Models;

/// <summary>
/// Represents a file attachment for an article
/// </summary>
public class Attachment : BaseEntity
{
    /// <summary>
    /// MIME content type of the attachment
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Locator reference for the attachment (e.g., URL, URI, etc.)
    /// </summary>
    public string Locator { get; set; } = string.Empty;

    /// <summary>
    /// Path to the attachment
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// ID of the article this attachment belongs to
    /// </summary>
    public Guid ArticleId { get; set; }

    /// <summary>
    /// Navigation property to the associated article
    /// </summary>
    public virtual Article? Article { get; set; }
}