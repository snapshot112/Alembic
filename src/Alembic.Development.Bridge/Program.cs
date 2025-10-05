using System.Text.Json;
using System.Text.Json.Serialization;
using Alembic.Agent.Core;
using Alembic.Agent.Core.Handlers;
using Alembic.Agent.Core.Helpers;
using Alembic.Agent.Core.Logging;
using Alembic.Agent.Core.Models;
using Alembic.Agent.Core.Secrets;
using Alembic.Development.Bridge.Logging;

namespace Alembic.Development.Bridge;

/*
 * The main application class for the Development Bridge.
 * This console app acts as a 'test harness' for the ActionDispatcher.
 */
public static class Program
{
    public static async Task Main(string[] args)
    {
        ILogger logger = new ConsoleLogger();
        
        try
        {
            var sourceRoot = ProjectEnvironment.SourceRoot;
            
            var userConfig = LoadUserConfig(logger);
            if (userConfig is null)
            {
                logger.LogError("Failed to load user configuration. Aborting.");
                return;
            }

            ISecretStore secretStore = new ProtectedDataSecretStore();
            var handlers = new List<IActionHandler>
            {
                new FileSystemActionHandler(sourceRoot, logger),
                new GitActionHandler(sourceRoot, logger),
                new DotnetActionHandler(sourceRoot, logger),
                new SecretActionHandler(logger, secretStore) // This handler should be tested before use.
            };
        
            var dispatcher = new ActionDispatcher(logger, userConfig, handlers);

            if (args.Length > 0)
            {
                var filePath = args[0];
                await ExecuteCommandFromFileAsync(filePath, dispatcher, logger);
            }
            else
            {
                await RunInteractiveLoopAsync(dispatcher, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"A fatal error occurred: {ex.Message}");
        }
    }
    
    private static UserConfig? LoadUserConfig(ILogger logger)
    {
        try
        {
            var configPath = Path.Combine(ProjectEnvironment.RepositoryRoot, "UserConfig", "settings.json");
            if (!File.Exists(configPath))
            {
                logger.LogError($"User configuration file not found at '{configPath}'.");
                return null;
            }

            var jsonContent = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            
            return JsonSerializer.Deserialize<UserConfig>(jsonContent, options);
        }
        catch (Exception ex)
        { 
            logger.LogError($"Error loading user configuration: {ex.Message}");
            return null;
        }
    }

    private static async Task ExecuteCommandFromFileAsync(string filePath, ActionDispatcher dispatcher, ILogger logger)
    {
        if (!SanitizationHelpers.TrySanitizePath(filePath, ProjectEnvironment.CommandsDirectory, out var sanitizedPath))
        {
            logger.LogError("Path traversal detected or path is invalid. Commands can only be loaded from the 'Commands' directory.");
            return;
        }

        if (!File.Exists(sanitizedPath))
        {
            logger.LogError($"Command file not found at '{sanitizedPath}'");
            return;
        }

        var jsonContent = await File.ReadAllTextAsync(sanitizedPath);
        await ProcessCommandInput(jsonContent, dispatcher, logger);
    }
    
    private static async Task RunInteractiveLoopAsync(ActionDispatcher dispatcher, ILogger logger)
    {
        logger.LogInfo("Alembic Development Bridge Initialized (Secure Action Model).");
        while (true)
        {
            Console.Write("> ");
            var input = await Console.In.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase)) { break; }
            await ProcessCommandInput(input, dispatcher, logger);
        }
        logger.LogInfo("Alembic Development Bridge Terminated.");
    }

    /*
     * Central logic to process a JSON command string from any source.
     * This method now correctly clones the JsonElement to ensure its lifetime
     * extends beyond the scope of the initial JsonDocument.
     */
    private static async Task ProcessCommandInput(string jsonInput, ActionDispatcher dispatcher, ILogger logger)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            using var jsonDoc = JsonDocument.Parse(jsonInput);
            
            switch (jsonDoc.RootElement.ValueKind)
            {
                case JsonValueKind.Array:
                {
                    logger.LogInfo($"Executing batch of {jsonDoc.RootElement.GetArrayLength()} commands...");
                    foreach (var command in jsonDoc.RootElement.EnumerateArray()
                                 .Select(element => element.Deserialize<Command>(options)).OfType<Command>())
                    {
                        await dispatcher.ExecuteActionAsync(command);
                    }
                    logger.LogInfo("Batch execution complete.");
                    break;
                }
                case JsonValueKind.Object:
                {
                    var command = jsonDoc.RootElement.Deserialize<Command>(options);
                    if (command != null)
                    {
                        await dispatcher.ExecuteActionAsync(command);
                    }

                    break;
                }
                default:
                    logger.LogError("Invalid JSON command format. Root must be an object or an array.");
                    break;
            }
        }
        catch (JsonException ex)
        {
            logger.LogError($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError($"An unexpected error occurred: {ex.Message}");
        }
    }
}

