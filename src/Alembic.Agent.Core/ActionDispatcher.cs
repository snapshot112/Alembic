using System.Reflection;
using Alembic.Agent.Core.Handlers;
using Alembic.Agent.Core.Logging;
using Alembic.Agent.Core.Models;

namespace Alembic.Agent.Core;

/*
 * The core class responsible for receiving commands and dispatching them
 * to the appropriate secure handlers. This is the "engine" of the Local Agent.
 */
public class ActionDispatcher(string rootDirectory, ILogger logger, UserConfig userConfig)
{
    private readonly List<IActionHandler> _handlers =
    [
        new FileSystemActionHandler(rootDirectory, logger),
        new GitActionHandler(rootDirectory, logger),
        new DotnetActionHandler(rootDirectory, logger)
    ];

    /*
     * Initializes a new instance of the ActionDispatcher class.
     * @param rootDirectory The secure root directory for all file and process operations.
     * @param logger The logging provider to use for all output.
     * @param userConfig The loaded user security configuration.
     */
    // Register all available handlers.

    /*
     * The primary public entry point for executing a command.
     * It performs a security check before finding and delegating to the correct handler.
     * @param command The command object to execute.
     */
    public async Task ExecuteActionAsync(Command command)
    {
        if (!IsActionAllowed(command.Action))
        {
            logger.LogError($"Action '{command.Action}' was blocked due to security settings.");
            return;
        }

        logger.LogInfo($"Executing action: {command.Action}");

        var handler = _handlers.FirstOrDefault(h => h.CanHandle(command.Action));
        if (handler != null)
        {
            await handler.HandleActionAsync(command);
        }
        else
        {
            logger.LogError($"No handler registered for action '{command.Action}'.");
        }
    }

    /*
     * Checks if an action is allowed to proceed based on its security level
     * and the user's configured verification requirements.
     * @param action The agent action to verify.
     * @returns True if the action is allowed, false otherwise.
     */
    private bool IsActionAllowed(AgentAction action)
    {
        var securityLevel = GetSecurityLevelForAction(action);
        
        if (!userConfig.VerificationSettings.TryGetValue(securityLevel, out var requiredMethod))
        {
            logger.LogError($"Security level '{securityLevel}' for action '{action}' is not defined in user configuration. Denying execution.");
            return false;
        }

        logger.LogInfo($"Action '{action}' requires '{requiredMethod}' verification as per user settings.");

        switch (requiredMethod)
        {
            case VerificationMethod.Password:
                logger.LogWarning("Verification Required: Please enter your password.");
                return true; 
            
            case VerificationMethod.Authenticator:
                logger.LogWarning("Verification Required: Please approve the request on your authenticator app.");
                return true;

            case VerificationMethod.None:
                return true; 
            
            default:
                logger.LogError($"Unknown verification method '{requiredMethod}' in user configuration. Denying execution.");
                return false;
        }
    }

    /*
     * Uses reflection to read the SecurityLevelAttribute from an AgentAction enum member.
     * @param action The action to inspect.
     * @returns The declared SecurityLevel.
     */
    private SecurityLevel GetSecurityLevelForAction(AgentAction action)
    {
        var memberInfo = typeof(AgentAction).GetMember(action.ToString()).FirstOrDefault();
        var attribute = memberInfo?.GetCustomAttribute<SecurityLevelAttribute>();
        
        return attribute?.Level ?? SecurityLevel.RequiresStrongAuthentication;
    }
}

