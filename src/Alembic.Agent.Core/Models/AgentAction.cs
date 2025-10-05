using System.Text.Json.Serialization;

namespace Alembic.Agent.Core.Models;

/*
 * Defines the complete set of actions the agent can perform.
 * Each action is decorated with a SecurityLevel attribute to define its risk.
 */
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentAction
{
    [SecurityLevel(SecurityLevel.RequiresConfirmation)]
    CreateFile,
    
    [SecurityLevel(SecurityLevel.None)]
    DotnetBuild,
    
    [SecurityLevel(SecurityLevel.RequiresConfirmation)]
    DotnetNewClasslib,
    
    [SecurityLevel(SecurityLevel.RequiresConfirmation)]
    GitCommit,
    
    [SecurityLevel(SecurityLevel.RequiresStrongAuthentication)]
    GitPush,
    
    [SecurityLevel(SecurityLevel.None)]
    GitStatus,
    
    [SecurityLevel(SecurityLevel.RequiresConfirmation)]
    GitAdd,
    
    [SecurityLevel(SecurityLevel.RequiresConfirmation)]
    GitReset,
    
    [SecurityLevel(SecurityLevel.RequiresStrongAuthentication)]
    GitCheckout,
    
    [SecurityLevel(SecurityLevel.None)]
    GitLog,
    
    [SecurityLevel(SecurityLevel.RequiresConfirmation)]
    GitBranch,
    
    [SecurityLevel(SecurityLevel.RequiresStrongAuthentication)]
    GitDeleteBranch
}
