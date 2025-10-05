namespace Alembic.Agent.Core.Models;

/*
 * A custom attribute used to decorate AgentAction enum members
 * with their corresponding security level. This provides a clean,
 * declarative way to manage action security.
 */
[AttributeUsage(AttributeTargets.Field)]
public class SecurityLevelAttribute(SecurityLevel level) : Attribute
{
    public SecurityLevel Level { get; } = level;
}
