using System.Text.Json.Serialization;

namespace Alembic.Agent.Core.Models;

/*
 * Defines a whitelist of executables that the agent is allowed to run.
 * This is a primary security measure to prevent arbitrary code execution.
 */
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AllowedExecutable
{
    Dotnet,
    Git
}