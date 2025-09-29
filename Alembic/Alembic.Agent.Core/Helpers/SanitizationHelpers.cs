namespace Alembic.Agent.Core.Helpers;

/*
 * Provides centralized methods for sanitizing and validating user-provided input
 * to prevent security vulnerabilities.
 */
public static class SanitizationHelpers
{
    /*
     * Validates a relative path to ensure it resolves to a location within a specified, secure base directory.
     * This is the primary defense against Path Traversal vulnerabilities.
     *
     * @param relativePath The user-provided relative path to validate.
     * @param allowedBaseDirectory The absolute path of the directory that the user path must be within.
     * @param sanitizedFullPath The resulting sanitized, absolute path if validation is successful.
     * @returns `true` if the path is safe, `false` otherwise.
     */
    public static bool TrySanitizePath(string? relativePath, string allowedBaseDirectory, out string sanitizedFullPath)
    {
        sanitizedFullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            // An empty path can sometimes default to the base directory, which is safe.
            relativePath = string.Empty;
        }

        try
        {
            var requestedFullPath = Path.GetFullPath(Path.Combine(allowedBaseDirectory, relativePath));

            if (requestedFullPath.StartsWith(allowedBaseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                sanitizedFullPath = requestedFullPath;
                return true;
            }
        }
        catch (Exception)
        {
            // Catches invalid path characters or other file system errors.
            return false;
        }

        return false;
    }
}