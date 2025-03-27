namespace ProPulse.Persistence.Models;

/// <summary>
/// Represents the possible states of an article
/// </summary>
public enum ArticleState
{
    /// <summary>
    /// Article is in draft state and not published
    /// </summary>
    Draft,
    
    /// <summary>
    /// Article is published and visible to users
    /// </summary>
    Published,
    
    /// <summary>
    /// Article has been retired and is no longer visible
    /// </summary>
    Retired
}