using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Alembic.Development.Bridge
{
    /// <summary>
    /// Represents a single instruction received from the user.
    /// Records are used for their simplicity and immutability.
    /// </summary>
    public record Command(string Action, string? Path, string? Content, string? Arguments);

    /// <summary>
    /// The main application class for the Development Bridge.
    /// This is a simple console app designed to bootstrap the development process
    /// by executing commands provided by the AI assistant.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Alembic Development Bridge Initialized.");
            Console.WriteLine("Ready to receive JSON commands. Type 'exit' to quit.");

            // The main application loop.
            while (true)
            {
                Console.Write("> ");
                var input = await Console.In.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                try
                {
                    // Use modern System.Text.Json for high-performance parsing.
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var command = JsonSerializer.Deserialize<Command>(input, options);

                    if (command == null || string.IsNullOrWhiteSpace(command.Action))
                    {
                        Console.WriteLine("Error: Invalid JSON or missing 'action' property.");
                        continue;
                    }
                    
                    // Dispatch the command to the appropriate handler.
                    await ExecuteCommandAsync(command);
                }
                catch (JsonException)
                {
                    Console.WriteLine("Error: Invalid JSON format.");
                }
                catch (Exception ex)
                {
                    // Catch-all for any other errors during execution.
                    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                }
            }

            Console.WriteLine("Alembic Development Bridge Terminated.");
        }

        /// <summary>
        /// Executes a command based on its 'Action' property.
        /// </summary>
        private static async Task ExecuteCommandAsync(Command command)
        {
            switch (command.Action.ToLowerInvariant())
            {
                case "create_file":
                    HandleCreateFile(command);
                    break;
                
                case "execute_command":
                    await HandleExecuteCommandAsync(command);
                    break;
                
                default:
                    Console.WriteLine($"Error: Unknown action '{command.Action}'.");
                    break;
            }
        }

        /// <summary>
        /// Handles the creation and writing of a file.
        /// </summary>
        private static void HandleCreateFile(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.Path))
            {
                Console.WriteLine("Error: 'path' is required for create_file action.");
                return;
            }

            try
            {
                // Ensure the directory exists before creating the file.
                var directory = Path.GetDirectoryName(command.Path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(command.Path, command.Content ?? string.Empty);
                Console.WriteLine($"Successfully created/updated file: {command.Path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating file {command.Path}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles the execution of a shell command.
        /// </summary>
        private static async Task HandleExecuteCommandAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.Arguments))
            {
                Console.WriteLine("Error: 'arguments' are required for execute_command action.");
                return;
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe", // Or "/bin/bash" on Linux/macOS
                Arguments = $"/c {command.Arguments}", // The /c argument tells cmd to run the command and then terminate
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = processStartInfo };
            
            Console.WriteLine($"Executing: {command.Arguments}");
            
            process.Start();
            
            // Asynchronously read the output and error streams.
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(output))
            {
                Console.WriteLine("Output:\n" + output);
            }

            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine("Error Output:\n" + error);
            }
            
            Console.WriteLine($"Command finished with exit code: {process.ExitCode}");
        }
    }
}
