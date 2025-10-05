using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Alembic.Agent.Core;
using Alembic.Agent.Core.Helpers;
using Alembic.Agent.Core.Logging;
using Alembic.Agent.Core.Models;
using Alembic.Development.Bridge.Logging;

namespace Alembic.Development.Bridge;

/*
 * The main application class for the Development Bridge.
 * This console app acts as a "test harness" for the ActionDispatcher,
 * loading user configuration and providing a way to send commands.
 */
public static class Program
{
    /*
     * The main entry point for the application.
     */
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
        
            var dispatcher = new ActionDispatcher(sourceRoot, logger, userConfig);

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
        catch (DirectoryNotFoundException ex)
        {
            logger.LogError(ex.Message);
        }
    }
    
    /*
     * Loads and deserializes the UserConfig from 'UserConfig/settings.json'.
     * @param logger The logger to use for reporting errors.
     * @returns The loaded UserConfig object, or null if loading fails.
     */
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

    /*
     * Reads, deserializes, and executes a single command from a given file path.
     */
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
    
    /*
     * Runs the interactive command loop, waiting for user input from the console.
     */
    private static async Task RunInteractiveLoopAsync(ActionDispatcher dispatcher, ILogger logger)
    {
        logger.LogInfo("Alembic Development Bridge Initialized (Secure Action Model).");
        logger.LogInfo("Ready to receive JSON commands. Type 'exit' to quit.");

        while (true)
        {
            Console.Write("> ");
            var input = await Console.In.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            await ProcessCommandInput(input, dispatcher, logger);
        }
        
        logger.LogInfo("Alembic Development Bridge Terminated.");
    }

    /*
     * Central logic to process a JSON command string from any source.
     * This method can now handle both a single command object and an array of commands.
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
            
            if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                logger.LogInfo($"Executing batch of {jsonDoc.RootElement.GetArrayLength()} commands...");
                foreach (var element in jsonDoc.RootElement.EnumerateArray())
                {
                    var command = element.Deserialize<Command>(options);
                    if (command != null)
                    {
                        await dispatcher.ExecuteActionAsync(command);
                    }
                }
                logger.LogInfo("Batch execution complete.");
            }
            else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var command = jsonDoc.RootElement.Deserialize<Command>(options);
                if (command != null)
                {
                    await dispatcher.ExecuteActionAsync(command);
                }
            }
            else
            {
                logger.LogError("Invalid JSON command format. Root must be an object or an array.");
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

