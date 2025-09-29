using System.Diagnostics;
using System.Text.Json;
using Alembic.Agent.Core.Helpers;
using Alembic.Agent.Core.Logging;

namespace Alembic.Agent.Core;

/*
 * Represents a single instruction. The Parameters property is a flexible JSON
 * element that allows each action to have its own unique set of arguments.
 */
public record Command(string Action, JsonElement Parameters);

/*
 * The core class responsible for receiving commands and dispatching them
 * to the appropriate secure handlers. This is the "engine" of the Local Agent.
 * @param rootDirectory The secure root directory for all file and process operations.
 */
public class ActionDispatcher(string rootDirectory, ILogger logger)
{
    /*
     * The primary public entry point for executing a command.
     * It routes a command to the appropriate secure handler based on its Action property.
     * @param command The command object to execute.
     */
    public async Task ExecuteActionAsync(Command command)
    {
        switch (command.Action.ToLowerInvariant())
        {
            case "create_file":
                HandleCreateFile(command.Parameters);
                break;
            case "dotnet_build":
                await HandleDotnetBuildAsync(command.Parameters);
                break;
            case "dotnet_new_classlib":
                await HandleDotnetNewClasslibAsync(command.Parameters);
                break;
            // Add more cases for git_commit, dotnet_run etc. as we need them.
            default:
                Console.WriteLine($"Error: Unknown action '{command.Action}'.");
                break;
        }
    }

    /*
     * Securely handles the 'create_file' action by using the central sanitization helper.
     * @param parameters The JSON parameters for the action, expecting 'path' and optional 'content'.
     */
    private void HandleCreateFile(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("path", out var pathProp) || pathProp.GetString() is not { } path)
        {
            logger.LogError("'path' parameter is required for create_file action.");
            return;
        }

        if (!SanitizationHelpers.TrySanitizePath(path, rootDirectory, out var sanitizedPath))
        {
            logger.LogError("Path traversal detected or path is invalid.");
            return;
        }
        
        var content = parameters.TryGetProperty("content", out var contentProp) 
            ? contentProp.GetString() ?? string.Empty 
            : string.Empty;

        try
        {
            var directory = Path.GetDirectoryName(sanitizedPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(sanitizedPath, content);
            logger.LogInfo($"Successfully created/updated file: {sanitizedPath}");
        }
        catch (Exception ex)
        {
            logger.LogError($"creating file {path}: {ex.Message}");
        }
    }
    
    /*
     * Securely handles executing the 'dotnet build' command.
     * @param parameters The JSON parameters for the action, expecting an optional 'projectPath'.
     */
    private async Task HandleDotnetBuildAsync(JsonElement parameters)
    {
        var projectPath = parameters.TryGetProperty("projectPath", out var pathProp) 
                          && pathProp.GetString() is { } path
            ? path
            : string.Empty;

        if (!SanitizationHelpers.TrySanitizePath(projectPath, rootDirectory, out _))
        {
            logger.LogError("Path traversal detected or path is invalid for projectPath.");
            return;
        }
        
        var arguments = $"build \"{projectPath}\"";
        await ExecuteProcessAsync("dotnet", arguments);
    }

    /*
     * Securely handles creating a new .NET class library. It ensures the project
     * is created within the designated 'src' directory.
     * @param parameters The JSON parameters for the action, expecting 'outputPath'.
     */
    private async Task HandleDotnetNewClasslibAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("outputPath", out var pathProp) || pathProp.GetString() is not { } outputPath)
        {
            logger.LogError("'outputPath' parameter is required for dotnet_new_classlib action.");
            return;
        }

        var srcDirectory = Path.Combine(rootDirectory, "src");
        if (!SanitizationHelpers.TrySanitizePath(outputPath, srcDirectory, out var sanitizedPath))
        {
            logger.LogError("Path traversal detected or path is invalid. Project must be created inside the 'src' directory.");
            return;
        }

        var arguments = $"new classlib -o \"{sanitizedPath}\"";
        await ExecuteProcessAsync("dotnet", arguments);
    }

    /*
     * A generalized, secure process executor. It runs a specified executable
     * with given arguments, always sandboxed to the project's root directory.
     * @param executable The command or application to run (e.g., "dotnet", "git").
     * @param arguments The arguments to pass to the executable.
     * @returns A Task representing the asynchronous operation.
     */
    private async Task ExecuteProcessAsync(string executable, string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = rootDirectory
        };

        var process = new Process { StartInfo = processStartInfo };
        
        logger.LogInfo($"Executing in '{rootDirectory}': {executable} {arguments}");
        
        process.Start();
        
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();

        if (!string.IsNullOrEmpty(output)) Console.WriteLine("Output:\n" + output);
        if (!string.IsNullOrEmpty(error)) Console.WriteLine("Error Output:\n" + error);
        
        Console.WriteLine($"Command finished with exit code: {process.ExitCode}");
    }
}

