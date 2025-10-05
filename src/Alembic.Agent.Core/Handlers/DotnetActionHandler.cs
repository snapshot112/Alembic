using System.Text.Json;
using Alembic.Agent.Core.Helpers;
using Alembic.Agent.Core.Logging;
using Alembic.Agent.Core.Models;

namespace Alembic.Agent.Core.Handlers;

/*
 * Handles all actions related to the .NET CLI.
 */
public class DotnetActionHandler(string rootDirectory, ILogger logger) : ProcessActionHandlerBase(rootDirectory, logger), IActionHandler
{
    public bool CanHandle(AgentAction action) =>
        action is AgentAction.DotnetBuild or AgentAction.DotnetNewClasslib;

    public async Task HandleActionAsync(Command command)
    {
        switch (command.Action)
        {
            case AgentAction.DotnetBuild:
                await HandleDotnetBuildAsync(command.Parameters);
                break;
            case AgentAction.DotnetNewClasslib:
                await HandleDotnetNewClasslibAsync(command.Parameters);
                break;
            default:
                Logger.LogError($"DotnetActionHandler cannot handle action '{command.Action}'.");
                break;
        }
    }
    
    public async Task HandleDotnetBuildAsync(JsonElement parameters)
    {
        var projectPath = parameters.TryGetProperty("projectPath", out var pathProp) 
                          && pathProp.GetString() is { } path
            ? path
            : string.Empty;

        if (!SanitizationHelpers.TrySanitizePath(projectPath, RootDirectory, out _))
        {
            Logger.LogError("Path traversal detected or path is invalid for projectPath.");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            await ExecuteProcessAsync(Models.AllowedExecutable.Dotnet, "build");
        }
        else
        {
            await ExecuteProcessAsync(Models.AllowedExecutable.Dotnet, "build", projectPath);
        }
    }

    public async Task HandleDotnetNewClasslibAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("outputPath", out var pathProp) || pathProp.GetString() is not { } outputPath)
        {
            Logger.LogError("'outputPath' parameter is required for dotnet_new_classlib action.");
            return;
        }

        var srcDirectory = Path.Combine(RootDirectory, "src"); // This assumes a consistent 'src' folder for new projects.
        if (!SanitizationHelpers.TrySanitizePath(outputPath, srcDirectory, out var sanitizedPath))
        {
            Logger.LogError("Path traversal detected or path is invalid. Project must be created inside the 'src' directory.");
            return;
        }

        await ExecuteProcessAsync(Models.AllowedExecutable.Dotnet, "new", "classlib", "-o", sanitizedPath);
    }
}