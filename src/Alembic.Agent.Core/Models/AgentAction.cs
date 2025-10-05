namespace Alembic.Agent.Core.Models;

/*
 * Defines the complete set of actions the agent can perform.
 * Each action is decorated with a SecurityLevel attribute to define its risk.
 */
public enum AgentAction
{
    // File System
    [SecurityLevel(SecurityLevel.RequiresConfirmation)]
    CreateFile,
    [SecurityLevel(SecurityLevel.None)]
    ReadFile,
    [SecurityLevel(SecurityLevel.RequiresConfirmation)]
    DeleteFile,
    [SecurityLevel(SecurityLevel.None)]
    ListDirectory,
    
    // .NET CLI
    [SecurityLevel(SecurityLevel.None)]
    DotnetBuild,
    [SecurityLevel(SecurityLevel.RequiresConfirmation)]
    DotnetNewClasslib,
    
    // Git
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
    GitDeleteBranch,
    [SecurityLevel(SecurityLevel.RequiresStrongAuthentication)]
    GitClean,
    
    // Secret Management
    [SecurityLevel(SecurityLevel.RequiresStrongAuthentication)]
    StoreSecret,
    [SecurityLevel(SecurityLevel.RequiresStrongAuthentication)]
    RetrieveSecret
}
