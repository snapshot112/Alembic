namespace Alembic.Agent.Core.Helpers;

/*
 * A static helper class to provide information about the project's runtime environment.
 * Its primary role is to reliably locate the project's root directory.
 */
public static class ProjectEnvironment
{
    private static string? _projectRoot;

    /*
     * Finds and returns the absolute path to the project's root directory.
     * The result is cached after the first call for performance.
     * The root is identified by searching upwards from the current execution
     * directory for a marker, such as the '.git' folder or 'Alembic.sln' file.
     *
     * @returns The full path to the project root.
     * @throws DirectoryNotFoundException if the root cannot be determined.
     */
    public static string GetProjectRoot()
    {
        // Return the cached result if we've already found it.
        if (!string.IsNullOrEmpty(_projectRoot))
        {
            return _projectRoot;
        }

        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        
        // Traverse up the directory tree until a marker is found.
        while (currentDirectory != null && 
               !Directory.Exists(Path.Combine(currentDirectory.FullName, ".git")) && 
               !File.Exists(Path.Combine(currentDirectory.FullName, "Alembic.sln")))
        {
            currentDirectory = currentDirectory.Parent;
        }

        if (currentDirectory == null)
        {
            // If we traverse all the way to the root without finding a marker, we're in an unknown location.
            throw new DirectoryNotFoundException("Could not find the project root directory. Ensure the application is run from within the project structure.");
        }

        _projectRoot = currentDirectory.FullName;
        return _projectRoot;
    }
}

