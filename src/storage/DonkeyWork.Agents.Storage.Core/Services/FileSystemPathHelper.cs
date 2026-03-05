using Microsoft.AspNetCore.StaticFiles;

namespace DonkeyWork.Agents.Storage.Core.Services;

public static class FileSystemPathHelper
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    /// <summary>
    /// Returns true if the object key represents a user file (no path separator).
    /// User files: "readme.txt" → true
    /// Path-keyed: "conversations/abc/image.png" → false
    /// </summary>
    public static bool IsUserFile(string objectKey)
    {
        return !objectKey.Contains('/');
    }

    /// <summary>
    /// Builds the canonical user directory path: {basePath}/{subPath}/{userId}/
    /// </summary>
    public static string GetUserDirectory(string basePath, string subPath, Guid userId)
    {
        return Path.Combine(basePath, subPath, userId.ToString());
    }

    /// <summary>
    /// Validates the filename and returns a safe full path within the user directory.
    /// Throws <see cref="ArgumentException"/> if the filename contains path traversal or separators.
    /// </summary>
    public static string GetSafeFilePath(string userDirectory, string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            throw new ArgumentException("Filename cannot be empty.", nameof(filename));

        if (filename.Contains('/') || filename.Contains('\\') || filename.Contains(".."))
            throw new ArgumentException("Filename contains invalid characters.", nameof(filename));

        var fullPath = Path.GetFullPath(Path.Combine(userDirectory, filename));
        var canonicalDir = Path.GetFullPath(userDirectory + Path.DirectorySeparatorChar);

        if (!fullPath.StartsWith(canonicalDir, StringComparison.Ordinal))
            throw new ArgumentException("Filename resolves outside the user directory.", nameof(filename));

        return fullPath;
    }

    /// <summary>
    /// Returns the MIME content type for a filename, defaulting to application/octet-stream.
    /// </summary>
    public static string GetContentType(string filename)
    {
        if (ContentTypeProvider.TryGetContentType(filename, out var contentType))
            return contentType;

        return "application/octet-stream";
    }
}
