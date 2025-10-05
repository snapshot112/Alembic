using System.Text.Json;
using Alembic.Agent.Core.Helpers;
using Alembic.Agent.Core.Logging;
using Alembic.Agent.Core.Models;

namespace Alembic.Agent.Core.Handlers;

/*
 * Handles all actions related to the Git version control system.
 */
public class GitActionHandler(string rootDirectory, ILogger logger) : ProcessActionHandlerBase(rootDirectory, logger), IActionHandler
{
    public bool CanHandle(AgentAction action) =>
        action is AgentAction.GitCommit or AgentAction.GitPush or AgentAction.GitStatus or AgentAction.GitAdd or
            AgentAction.GitReset or AgentAction.GitCheckout or AgentAction.GitLog or AgentAction.GitBranch or
            AgentAction.GitDeleteBranch or AgentAction.GitClean;

    public async Task HandleActionAsync(Command command)
    {
        switch (command.Action)
        {
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
            case AgentAction.GitClean:
                await HandleGitCleanAsync();
                break;
            default:
                Logger.LogError($"GitActionHandler cannot handle action '{command.Action}'.");
                break;
        }
    }
    public async Task HandleGitCommitAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("message", out var messageProp) || messageProp.GetString() is not { } message || string.IsNullOrWhiteSpace(message))
        {
            Logger.LogError("'message' parameter is required and cannot be empty for git_commit action.");
            return;
        }

        var stageAll = parameters.TryGetProperty("stageAll", out var stageAllProp) && stageAllProp.GetBoolean();

        if (stageAll)
        {
            await ExecuteProcessAsync(Models.AllowedExecutable.Git, "add", ".");
        }
        
        await ExecuteProcessAsync(Models.AllowedExecutable.Git, "commit", "-m", message);
    }

    public async Task HandleGitPushAsync()
    {
        await ExecuteProcessAsync(Models.AllowedExecutable.Git, "push");
    }

    public async Task HandleGitStatusAsync()
    {
        await ExecuteProcessAsync(Models.AllowedExecutable.Git, "status");
    }

    public async Task HandleGitAddAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("filePath", out var pathProp) || pathProp.GetString() is not { } filePath)
        {
            Logger.LogError("'filePath' parameter is required for git_add action.");
            return;
        }
        
        if (!SanitizationHelpers.TrySanitizePath(filePath, RootDirectory, out _))
        {
            Logger.LogError("Path traversal detected or path is invalid for filePath.");
            return;
        }
        
        await ExecuteProcessAsync(Models.AllowedExecutable.Git, "add", filePath);
    }

    public async Task HandleGitResetAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("filePath", out var pathProp) || pathProp.GetString() is not { } filePath)
        {
            Logger.LogError("'filePath' parameter is required for git_reset action.");
            return;
        }

        if (!SanitizationHelpers.TrySanitizePath(filePath, RootDirectory, out _))
        {
            Logger.LogError("Path traversal detected or path is invalid for filePath.");
            return;
        }
        
        await ExecuteProcessAsync(Models.AllowedExecutable.Git, "reset", "HEAD", "--", filePath);
    }
    
    public async Task HandleGitCheckoutAsync(JsonElement parameters)
    {
        if (parameters.TryGetProperty("branchName", out var branchProp) && branchProp.GetString() is { } branchName)
        {
            await ExecuteProcessAsync(Models.AllowedExecutable.Git, "checkout", branchName);
        }
        else if (parameters.TryGetProperty("filePath", out var pathProp) && pathProp.GetString() is { } filePath)
        {
            if (!SanitizationHelpers.TrySanitizePath(filePath, RootDirectory, out _))
            {
                Logger.LogError("Path traversal detected or path is invalid for filePath.");
                return;
            }
            await ExecuteProcessAsync(Models.AllowedExecutable.Git, "checkout", "--", filePath);
        }
        else
        {
            Logger.LogError("Either 'branchName' or 'filePath' parameter is required for git_checkout action.");
        }
    }
    
    public async Task HandleGitLogAsync()
    {
        await ExecuteProcessAsync(Models.AllowedExecutable.Git, "log", "--oneline", "-n", "10");
    }

    public async Task HandleGitBranchAsync(JsonElement parameters)
    {
        if (parameters.TryGetProperty("branchName", out var branchProp) && branchProp.GetString() is { } branchName)
        {
            await ExecuteProcessAsync(Models.AllowedExecutable.Git, "branch", branchName);
        }
        else
        {
            await ExecuteProcessAsync(Models.AllowedExecutable.Git, "branch");
        }
    }

    public async Task HandleGitDeleteBranchAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("branchName", out var branchProp) || branchProp.GetString() is not { } branchName)
        {
            Logger.LogError("'branchName' parameter is required for git_delete_branch action.");
            return;
        }
        
        await ExecuteProcessAsync(Models.AllowedExecutable.Git, "branch", "-d", branchName);
    }

    public async Task HandleGitCleanAsync()
    {
        await ExecuteProcessAsync(Models.AllowedExecutable.Git, "clean", "-fd");
    }
}
