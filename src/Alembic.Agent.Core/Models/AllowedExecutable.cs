namespace Alembic.Agent.Core.Models;

/*
 * Defines a whitelist of executables that the agent is allowed to run.
 * This is a primary security measure to prevent arbitrary code execution.
 */
public enum AllowedExecutable
{
    Dotnet,
    Git
}