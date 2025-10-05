using Alembic.Agent.Core.Models;

namespace Alembic.Agent.Core.Handlers;

/*
 * Defines a contract for a specialized action handler.
 */
public interface IActionHandler
{
    /*
     * Checks if this handler is responsible for a given action.
     * @param action The action to check.
     * @returns True if the handler can execute the action, false otherwise.
     */
    bool CanHandle(AgentAction action);

    /*
     * Executes the logic for a given command.
     * @param command The command to execute.
     */
    Task HandleActionAsync(Command command);
}