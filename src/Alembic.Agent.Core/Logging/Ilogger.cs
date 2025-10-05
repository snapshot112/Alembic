namespace Alembic.Agent.Core.Logging;

/*
 * Defines a contract for a generic logging provider.
 * This abstraction allows the core agent logic to be decoupled
 * from any specific logging implementation (e.g., console, file, etc.).
 */
public interface ILogger
{
    /*
     * Logs an informational message.
     * @param message The message to log.
     */
    void LogInfo(string message);
    
    /*
     * Logs a warning message.
     * @param message The error message to log.
     */
    void LogWarning(string message);
    
    /*
     * Logs an error message.
     * @param message The error message to log.
     */
    void LogError(string message);
    
}