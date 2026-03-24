namespace DonkeyWork.Agents.Projects.Contracts.Helpers;

/// <summary>
/// Helper class for truncating content fields and supporting chunked reading.
/// </summary>
public static class ContentTruncationHelper
{
    /// <summary>
    /// Default maximum length for content previews in summary responses.
    /// </summary>
    public const int DefaultPreviewLength = 60;

    /// <summary>
    /// Truncates content to the specified maximum length, appending "..." if truncated.
    /// </summary>
    public static string? TruncateContent(string? content, int maxLength = DefaultPreviewLength)
    {
        if (string.IsNullOrEmpty(content))
            return content;
        if (content.Length <= maxLength)
            return content;
        return content[..maxLength] + "...";
    }

    /// <summary>
    /// Gets the total character length of the content.
    /// </summary>
    public static int GetContentLength(string? content)
    {
        return content?.Length ?? 0;
    }

    /// <summary>
    /// Applies offset/length chunking to content for paginated reading.
    /// Returns the full content when both offset and length are null.
    /// </summary>
    public static string? ApplyChunking(string? content, int? offset, int? length)
    {
        if (content == null)
            return null;
        if (offset == null && length == null)
            return content;
        var start = Math.Min(offset ?? 0, content.Length);
        var remaining = content.Length - start;
        var take = length.HasValue ? Math.Min(length.Value, remaining) : remaining;
        return content.Substring(start, take);
    }
}
