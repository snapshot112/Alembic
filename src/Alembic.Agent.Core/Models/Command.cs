using System.Text.Json;

namespace Alembic.Agent.Core.Models;

/*
 * Represents a single instruction. The Action property uses the AgentAction
 * enum for type safety, ensuring only valid actions can be represented.
 */
public record Command(AgentAction Action, JsonElement Parameters);
