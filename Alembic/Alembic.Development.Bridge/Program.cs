using System.Text.Json;
using Alembic.Agent.Core;
using Alembic.Agent.Core.Helpers;
using Alembic.Agent.Core.Logging;
using Alembic.Development.Bridge.Logging;

namespace Alembic.Development.Bridge;

/*
 * The main application class for the Development Bridge.
 * This console app acts as a "test harness" for the ActionDispatcher,
 * providing a console-based logger and a way to send commands.
 */
public static class Program
{
    /*
     * The main entry point for the application.
     * It can run in two modes:
     * 1. Interactive Mode (no arguments): Runs the primary command processing loop.
     * 2. File Mode (one argument): Reads and executes a single command from a specified file path.
     */
    public static async Task Main(string[] args)
    {
        var rootDirectory = ProjectEnvironment.GetProjectRoot();
        
        // 1. Create the concrete logger implementation.
        ILogger logger = new ConsoleLogger();

        // 2. Inject the logger into the ActionDispatcher.
        var dispatcher = new ActionDispatcher(rootDirectory, logger);

        if (args.Length > 0)
        {
            // File Mode: Execute a command from a file and exit.
            var filePath = args[0];
            await ExecuteCommandFromFileAsync(filePath, dispatcher, logger);
        }
        else
        {
            // Interactive Mode: Run the command loop.
            await RunInteractiveLoopAsync(dispatcher, logger);
        }
    }

    /*
     * Reads, deserializes, and executes a single command from a given file path.
     */
    private static async Task ExecuteCommandFromFileAsync(string filePath, ActionDispatcher dispatcher, ILogger logger)
    {
        if (!SanitizationHelpers.TrySanitizePath(filePath,
                Path.Combine(ProjectEnvironment.GetProjectRoot(), "/Commands"), out var sanitizedPath))
        {
            logger.LogError("Path traversal detected or path is invalid.");
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
     * Central logic to process a JSON command string from any source (file or console).
     */
    private static async Task ProcessCommandInput(string jsonInput, ActionDispatcher dispatcher, ILogger logger)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var command = JsonSerializer.Deserialize<Command>(jsonInput, options);

            if (command == null || string.IsNullOrWhiteSpace(command.Action))
            {
                logger.LogError("Invalid JSON or missing 'action' property.");
                return;
            }
                    
            await dispatcher.ExecuteActionAsync(command);
        }
        catch (JsonException)
        {
            logger.LogError("Invalid JSON format.");
        }
        catch (Exception ex)
        {
            logger.LogError($"An unexpected error occurred: {ex.Message}");
        }
    }
}

