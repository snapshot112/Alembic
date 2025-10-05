namespace Alembic.Agent.Core.Helpers;

/*
 * A static helper class to provide information about the project's runtime environment.
 * Its primary role is to reliably locate the project's root directory.
 */
public static class ProjectEnvironment
{
    
    private static string? _repositoryRoot;
    private static string? _sourceRoot;

    /*
     * Gets the absolute path to the repository's root directory.
     * The root is identified by searching upwards for the '.git' folder.
     * The result is cached for performance.
     *
     * @returns The full path to the repository root.
     * @throws DirectoryNotFoundException if the root cannot be determined.
     */
    public static string RepositoryRoot
    {
        get
        {
            if (!string.IsNullOrEmpty(_repositoryRoot))
            {
                return _repositoryRoot;
            }

            var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
            while (currentDirectory != null && !Directory.Exists(Path.Combine(currentDirectory.FullName, ".git")))
            {
                currentDirectory = currentDirectory.Parent;
            }

            if (currentDirectory == null)
            {
                throw new DirectoryNotFoundException("Could not find the repository root directory (marker: .git folder).");
            }

            _repositoryRoot = currentDirectory.FullName;
            return _repositoryRoot;
        }
    }
    
    /*
     * Gets the absolute path to the source code ('src') directory, which contains the solution file.
     * The root is identified by searching upwards for the 'Alembic.sln' file.
     * The result is cached for performance.
     *
     * @returns The full path to the 'src' directory.
     * @throws DirectoryNotFoundException if the root cannot be determined.
     */
    public static string SourceRoot
    {
        get
        {
            if (!string.IsNullOrEmpty(_sourceRoot))
            {
                return _sourceRoot;
            }
            
            var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
            while (currentDirectory != null && !File.Exists(Path.Combine(currentDirectory.FullName, "Alembic.sln")))
            {
                currentDirectory = currentDirectory.Parent;
            }

            if (currentDirectory == null)
            {
                throw new DirectoryNotFoundException("Could not find the source root directory (marker: Alembic.sln file).");
            }

            _sourceRoot = currentDirectory.FullName;
            return _sourceRoot;
        }
    }

    /*
     * Gets the full path to the commands directory.
     * @returns The path to 'Commands' within the repository root.
     */
    public static string CommandsDirectory => Path.Combine(RepositoryRoot, "Commands");
    
    /*
     * Gets the full path to the user configuration file.
     * @returns The path to 'UserConfig/settings.json' within the project root.
     */
    public static string GetUserConfigPath() => Path.Combine(RepositoryRoot, "UserConfig", "settings.json");
}

