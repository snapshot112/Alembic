using System.Text.Json.Serialization;

namespace Alembic.Agent.Core.Models;

/*
 * Defines the potential security risk associated with an agent action.
 */
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SecurityLevel
{
    // No risk. Read-only operations or safe local processes.
    None,
    
    // Moderate risk. Actions that modify the local file system.
    RequiresConfirmation,
    
    // High risk. Actions that could involve credentials or push changes to remote systems.
    RequiresStrongAuthentication
}