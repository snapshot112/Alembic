using System.Text;
using System.Text.Json;
using Alembic.Agent.Core.Helpers;
using Alembic.Agent.Core.Logging;
using Alembic.Agent.Core.Models;

namespace Alembic.Agent.Core.Handlers;

/*
 * Handles all actions related to the local file system.
 */
public class FileSystemActionHandler(string rootDirectory, ILogger logger) : ProcessActionHandlerBase(rootDirectory, logger), IActionHandler
{
    public bool CanHandle(AgentAction action) =>
        action is AgentAction.CreateFile or AgentAction.ReadFile or AgentAction.DeleteFile or AgentAction.ListDirectory;

    public Task HandleActionAsync(Command command)
    {
        switch (command.Action)
        {
            case AgentAction.CreateFile:
                HandleCreateFile(command.Parameters);
                return Task.CompletedTask;
            case AgentAction.ReadFile:
                return HandleReadFileAsync(command.Parameters);
            case AgentAction.DeleteFile:
                HandleDeleteFile(command.Parameters);
                return Task.CompletedTask;
            case AgentAction.ListDirectory:
                HandleListDirectory(command.Parameters);
                return Task.CompletedTask;
            default:
                Logger.LogError($"FileSystemActionHandler cannot handle action '{command.Action}'.");
                return Task.CompletedTask;
        }
    }
    public void HandleCreateFile(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("path", out var pathProp) || pathProp.GetString() is not { } path)
        {
            Logger.LogError("'path' parameter is required for create_file action.");
            return;
        }

        if (!SanitizationHelpers.TrySanitizePath(path, RootDirectory, out var sanitizedPath))
        {
            Logger.LogError("Path traversal detected or path is invalid.");
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
            Logger.LogInfo($"Successfully created/updated file: {sanitizedPath}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"creating file {path}: {ex.Message}");
        }
    }

    public async Task HandleReadFileAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("filePath", out var pathProp) || pathProp.GetString() is not { } filePath)
        {
            Logger.LogError("'filePath' parameter is required for read_file action.");
            return;
        }

        if (!SanitizationHelpers.TrySanitizePath(filePath, RootDirectory, out var sanitizedPath))
        {
            Logger.LogError("Path traversal detected or path is invalid for filePath.");
            return;
        }

        try
        {
            if (!File.Exists(sanitizedPath))
            {
                Logger.LogError($"File not found: {sanitizedPath}");
                return;
            }

            var content = await File.ReadAllTextAsync(sanitizedPath);
            Logger.LogInfo($"Content of '{filePath}':\n---\n{content}\n---");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error reading file {filePath}: {ex.Message}");
        }
    }

    public void HandleDeleteFile(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("filePath", out var pathProp) || pathProp.GetString() is not { } filePath)
        {
            Logger.LogError("'filePath' parameter is required for delete_file action.");
            return;
        }

        if (!SanitizationHelpers.TrySanitizePath(filePath, RootDirectory, out var sanitizedPath))
        {
            Logger.LogError("Path traversal detected or path is invalid for filePath.");
            return;
        }

        try
        {
            if (!File.Exists(sanitizedPath))
            {
                Logger.LogWarning($"File to delete does not exist: {sanitizedPath}");
                return;
            }

            File.Delete(sanitizedPath);
            Logger.LogInfo($"Successfully deleted file: {sanitizedPath}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error deleting file {filePath}: {ex.Message}");
        }
    }

    public void HandleListDirectory(JsonElement parameters)
    {
        var directoryPath = parameters.TryGetProperty("directoryPath", out var pathProp) && pathProp.GetString() is { } path
            ? path
            : string.Empty;

        if (!SanitizationHelpers.TrySanitizePath(directoryPath, RootDirectory, out var sanitizedPath))
        {
            Logger.LogError("Path traversal detected or path is invalid for directoryPath.");
            return;
        }

        try
        {
            if (!Directory.Exists(sanitizedPath))
            {
                Logger.LogError($"Directory not found: {sanitizedPath}");
                return;
            }

            var output = new StringBuilder();
            output.AppendLine($"Contents of '{directoryPath}':");

            foreach (var dir in Directory.GetDirectories(sanitizedPath))
            {
                output.AppendLine($"  [DIR]  {Path.GetFileName(dir)}");
            }
            foreach (var file in Directory.GetFiles(sanitizedPath))
            {
                output.AppendLine($"  [FILE] {Path.GetFileName(file)}");
            }
            
            Logger.LogInfo(output.ToString());
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error listing directory {directoryPath}: {ex.Message}");
        }
    }
}
