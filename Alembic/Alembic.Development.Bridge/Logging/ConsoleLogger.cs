using Alembic.Agent.Core.Logging;

namespace Alembic.Development.Bridge.Logging;

/*
 * A simple implementation of the ILogger interface that writes
 * messages to the standard console output.
 */
public class ConsoleLogger : ILogger
{
    /*
     * Logs an informational message to the console in the default color.
     * @param message The message to log.
     */
    public void LogInfo(string message)
    {
        Console.WriteLine(message);
    }

    /*
     * Logs an error message to the console in a distinct red color.
     * @param message The error message to log.
     */
    public void LogWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Error: {message}");
        Console.ResetColor();
    }

    /*
     * Logs an error message to the console in a distinct red color.
     * @param message The error message to log.
     */
    public void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {message}");
        Console.ResetColor();
    }
}