using System.Diagnostics;
using System.Text.Json;
using Alembic.Agent.Core.Helpers;
using Alembic.Agent.Core.Logging;
using Alembic.Agent.Core.Models;

namespace Alembic.Agent.Core;

/*
 * The core class responsible for receiving commands and dispatching them
 * to the appropriate secure handlers. This is the "engine" of the Local Agent.
 * @param rootDirectory The secure root directory for all file and process operations.
 * @param logger The logging provider to use for all output.
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
        switch (command.Action)
        {
            case AgentAction.CreateFile:
                HandleCreateFile(command.Parameters);
                break;
            case AgentAction.DotnetBuild:
                await HandleDotnetBuildAsync(command.Parameters);
                break;
            case AgentAction.DotnetNewClasslib:
                await HandleDotnetNewClasslibAsync(command.Parameters);
                break;
            case AgentAction.GitCommit:
                await HandleGitCommitAsync(command.Parameters);
                break;
            default:
                logger.LogError($"Unknown action '{command.Action}'.");
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
        
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            await ExecuteProcessAsync(AllowedExecutable.Dotnet, "build");
        }
        else
        {
            await ExecuteProcessAsync(AllowedExecutable.Dotnet, "build", projectPath);
        }
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

        await ExecuteProcessAsync(AllowedExecutable.Dotnet, "new", "classlib", "-o", sanitizedPath);
    }

    /*
     * Securely handles executing a 'git commit' command by passing arguments
     * directly to the process, avoiding shell interpretation.
     * @param parameters The JSON parameters, expecting 'message' and optional 'stageAll'.
     */
    private async Task HandleGitCommitAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("message", out var messageProp) || messageProp.GetString() is not { } message || string.IsNullOrWhiteSpace(message))
        {
            logger.LogError("'message' parameter is required and cannot be empty for git_commit action.");
            return;
        }

        var stageAll = parameters.TryGetProperty("stageAll", out var stageAllProp) && stageAllProp.GetBoolean();

        if (stageAll)
        {
            // First, stage all changes.
            await ExecuteProcessAsync(AllowedExecutable.Git, "add", ".");
        }
        
        // The 'message' is passed as a separate, literal argument, making this inherently secure.
        await ExecuteProcessAsync(AllowedExecutable.Git, "commit", "-m", message);
    }

    /*
     * A generalized, secure process executor. It runs a specified executable
     * from a whitelist with a list of arguments, always sandboxed to the project's root directory.
     * @param executable The whitelisted command to run.
     * @param arguments The list of arguments to pass to the executable.
     * @returns A Task representing the asynchronous operation.
     */
    private async Task ExecuteProcessAsync(AllowedExecutable executable, params string[] arguments)
    {
        var executableName = executable.ToString().ToLowerInvariant();
        
        var processStartInfo = new ProcessStartInfo
        {
            FileName = executableName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = rootDirectory
        };

        foreach (var arg in arguments)
        {
            processStartInfo.ArgumentList.Add(arg);
        }

        var process = new Process { StartInfo = processStartInfo };
        
        var commandForLog = $"{executableName} {string.Join(" ", arguments.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}";
        logger.LogInfo($"Executing in '{rootDirectory}': {commandForLog}");
        
        process.Start();
        
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();

        if (!string.IsNullOrEmpty(output)) logger.LogInfo("Output:\n" + output);
        if (!string.IsNullOrEmpty(error)) logger.LogError("Error Output:\n" + error);
        
        logger.LogInfo($"Command finished with exit code: {process.ExitCode}");
    }
}

