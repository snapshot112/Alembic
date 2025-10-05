using System.Reflection;
using Alembic.Agent.Core.Handlers;
using Alembic.Agent.Core.Logging;
using Alembic.Agent.Core.Models;

namespace Alembic.Agent.Core;

/*
 * The core class responsible for receiving commands and dispatching them
 * to the appropriate secure handlers. This is the 'engine' of the Local Agent.
 */
public class ActionDispatcher(ILogger logger, UserConfig userConfig, IEnumerable<IActionHandler> handlers)
{
    private readonly IReadOnlyList<IActionHandler> _handlers = handlers.ToList();

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

    private SecurityLevel GetSecurityLevelForAction(AgentAction action)
    {
        var memberInfo = typeof(AgentAction).GetMember(action.ToString()).FirstOrDefault();
        var attribute = memberInfo?.GetCustomAttribute<SecurityLevelAttribute>();
        
        return attribute?.Level ?? SecurityLevel.RequiresStrongAuthentication;
    }
}
