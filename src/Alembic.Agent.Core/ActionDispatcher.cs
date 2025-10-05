using System.Diagnostics;
using System.Reflection;
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
public class ActionDispatcher(string rootDirectory, ILogger logger, UserConfig userConfig)
{
    private readonly string _rootDirectory = rootDirectory;
    private readonly ILogger _logger = logger;
    private readonly UserConfig _userConfig = userConfig;
    /*
     * The primary public entry point for executing a command.
     * It routes a command to the appropriate secure handler based on its Action property.
     * @param command The command object to execute.
     */
    public async Task ExecuteActionAsync(Command command)
    {
        // 1. Perform Security Check
        if (!IsActionAllowed(command.Action))
        {
            _logger.LogError($"Action '{command.Action}' was blocked due to security settings.");
            return;
        }
        
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
            case AgentAction.GitPush:
                await HandleGitPushAsync();
                break;
            case AgentAction.GitStatus:
                await HandleGitStatusAsync();
                break;
            case AgentAction.GitAdd:
                await HandleGitAddAsync(command.Parameters);
                break;
            case AgentAction.GitReset:
                await HandleGitResetAsync(command.Parameters);
                break;
            case AgentAction.GitCheckout:
                await HandleGitCheckoutAsync(command.Parameters);
                break;
            case AgentAction.GitLog:
                await HandleGitLogAsync();
                break;
            case AgentAction.GitBranch:
                await HandleGitBranchAsync(command.Parameters);
                break;
            case AgentAction.GitDeleteBranch:
                await HandleGitDeleteBranchAsync(command.Parameters);
                break;
            default:
                _logger.LogError($"Unknown action '{command.Action}'.");
                break;
        }
    }
    
    /*
     * Checks if an action is allowed to proceed based on its security level
     * and the user's configured verification requirements.
     * @param action The agent action to verify.
     * @returns True if the action is allowed, false otherwise.
     */
    private bool IsActionAllowed(AgentAction action)
    {
        var securityLevel = GetSecurityLevelForAction(action);
        
        if (!_userConfig.VerificationSettings.TryGetValue(securityLevel, out var requiredMethod))
        {
            _logger.LogError($"Security level '{securityLevel}' for action '{action}' is not defined in user configuration. Denying execution.");
            return false;
        }

        _logger.LogInfo($"Action '{action}' requires '{requiredMethod}' verification as per user settings.");

        // This is where the actual verification logic (e.g., password prompt) will go.
        // For now, we simulate the flow and log the requirement.
        switch (requiredMethod)
        {
            case VerificationMethod.Password:
                _logger.LogWarning("Verification Required: Please enter your password.");
                return true; // Simulating success for now.
            case VerificationMethod.Authenticator:
                _logger.LogWarning("Verification Required: Please approve the request on your authenticator app.");
                return true; // Simulating success for now.
            case VerificationMethod.None:
                return true; // No verification needed.
            default:
                return false;
        }
    }

    /*
     * Uses reflection to read the SecurityLevelAttribute from an AgentAction enum member.
     * @param action The action to inspect.
     * @returns The declared SecurityLevel.
     */
    private SecurityLevel GetSecurityLevelForAction(AgentAction action)
    {
        var memberInfo = typeof(AgentAction).GetMember(action.ToString()).FirstOrDefault();
        var attribute = memberInfo?.GetCustomAttribute<SecurityLevelAttribute>();
        
        return attribute?.Level ?? SecurityLevel.RequiresStrongAuthentication;
    }

    /*
     * Securely handles the 'create_file' action by using the central sanitization helper.
     * @param parameters The JSON parameters for the action, expecting 'path' and optional 'content'.
     */
    private void HandleCreateFile(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("path", out var pathProp) || pathProp.GetString() is not { } path)
        {
            _logger.LogError("'path' parameter is required for create_file action.");
            return;
        }

        if (!SanitizationHelpers.TrySanitizePath(path, _rootDirectory, out var sanitizedPath))
        {
            _logger.LogError("Path traversal detected or path is invalid.");
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
            _logger.LogInfo($"Successfully created/updated file: {sanitizedPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"creating file {path}: {ex.Message}");
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

        if (!SanitizationHelpers.TrySanitizePath(projectPath, _rootDirectory, out _))
        {
            _logger.LogError("Path traversal detected or path is invalid for projectPath.");
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
            _logger.LogError("'outputPath' parameter is required for dotnet_new_classlib action.");
            return;
        }

        var srcDirectory = Path.Combine(_rootDirectory, "src");
        if (!SanitizationHelpers.TrySanitizePath(outputPath, srcDirectory, out var sanitizedPath))
        {
            _logger.LogError("Path traversal detected or path is invalid. Project must be created inside the 'src' directory.");
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
            _logger.LogError("'message' parameter is required and cannot be empty for git_commit action.");
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
     * Securely handles executing the 'git push' command.
     */
    private async Task HandleGitPushAsync()
    {
        await ExecuteProcessAsync(AllowedExecutable.Git, "push");
    }
    
    /*
     * Securely handles executing the 'git status' command.
     */
    private async Task HandleGitStatusAsync()
    {
        await ExecuteProcessAsync(AllowedExecutable.Git, "status");
    }
    
    /*
     * Securely handles executing the 'git add' command for a specific file path.
     * @param parameters The JSON parameters, expecting 'filePath'.
     */
    private async Task HandleGitAddAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("filePath", out var pathProp) || pathProp.GetString() is not { } filePath)
        {
            _logger.LogError("'filePath' parameter is required for git_add action.");
            return;
        }
        
        // Sanitize the file path to ensure it's within the project boundary.
        if (!SanitizationHelpers.TrySanitizePath(filePath, _rootDirectory, out _))
        {
            _logger.LogError("Path traversal detected or path is invalid for filePath.");
            return;
        }
        
        await ExecuteProcessAsync(AllowedExecutable.Git, "add", filePath);
    }
    
    /*
     * Securely handles executing 'git reset' to unstage a file.
     * @param parameters The JSON parameters, expecting 'filePath'.
     */
    private async Task HandleGitResetAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("filePath", out var pathProp) || pathProp.GetString() is not { } filePath)
        {
            _logger.LogError("'filePath' parameter is required for git_reset action.");
            return;
        }

        if (!SanitizationHelpers.TrySanitizePath(filePath, _rootDirectory, out _))
        {
            _logger.LogError("Path traversal detected or path is invalid for filePath.");
            return;
        }
        
        await ExecuteProcessAsync(AllowedExecutable.Git, "reset", "HEAD", "--", filePath);
    }
    
    /*
     * Securely handles 'git checkout' to switch branches or discard file changes.
     * @param parameters The JSON parameters, expecting either 'branchName' or 'filePath'.
     */
    private async Task HandleGitCheckoutAsync(JsonElement parameters)
    {
        if (parameters.TryGetProperty("branchName", out var branchProp) && branchProp.GetString() is { } branchName)
        {
            await ExecuteProcessAsync(AllowedExecutable.Git, "checkout", branchName);
        }
        else if (parameters.TryGetProperty("filePath", out var pathProp) && pathProp.GetString() is { } filePath)
        {
            if (!SanitizationHelpers.TrySanitizePath(filePath, _rootDirectory, out _))
            {
                _logger.LogError("Path traversal detected or path is invalid for filePath.");
                return;
            }
            await ExecuteProcessAsync(AllowedExecutable.Git, "checkout", "--", filePath);
        }
        else
        {
            _logger.LogError("Either 'branchName' or 'filePath' parameter is required for git_checkout action.");
        }
    }
    
    /*
     * Securely handles executing 'git log'.
     */
    private async Task HandleGitLogAsync()
    {
        await ExecuteProcessAsync(AllowedExecutable.Git, "log", "--oneline", "-n", "10");
    }

    /*
     * Securely handles listing branches or creating a new branch.
     * @param parameters The JSON parameters, expecting an optional 'branchName'.
     */
    private async Task HandleGitBranchAsync(JsonElement parameters)
    {
        if (parameters.TryGetProperty("branchName", out var branchProp) && branchProp.GetString() is { } branchName)
        {
            await ExecuteProcessAsync(AllowedExecutable.Git, "branch", branchName);
        }
        else
        {
            await ExecuteProcessAsync(AllowedExecutable.Git, "branch");
        }
    }

    /*
     * Securely handles deleting a branch.
     * @param parameters The JSON parameters, expecting 'branchName'.
     */
    private async Task HandleGitDeleteBranchAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("branchName", out var branchProp) || branchProp.GetString() is not { } branchName)
        {
            _logger.LogError("'branchName' parameter is required for git_delete_branch action.");
            return;
        }
        
        await ExecuteProcessAsync(AllowedExecutable.Git, "branch", "-d", branchName);
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
            WorkingDirectory = _rootDirectory
        };

        foreach (var arg in arguments)
        {
            processStartInfo.ArgumentList.Add(arg);
        }

        var process = new Process { StartInfo = processStartInfo };
        
        var commandForLog = $"{executableName} {string.Join(" ", arguments.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}";
        _logger.LogInfo($"Executing in '{_rootDirectory}': {commandForLog}");
        
        process.Start();
        
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();

        if (!string.IsNullOrEmpty(output)) _logger.LogInfo("Output:\n" + output);
        if (!string.IsNullOrEmpty(error)) _logger.LogError("Error Output:\n" + error);
        
        _logger.LogInfo($"Command finished with exit code: {process.ExitCode}");
    }
}

