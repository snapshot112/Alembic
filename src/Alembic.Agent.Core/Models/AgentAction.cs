namespace Alembic.Agent.Core.Models;

/*
 * Defines the complete set of actions the agent can perform.
 * Using an enum provides compile-time safety and self-documentation.
 * * NOTE: This enum should eventually be moved to its own file within a 'Models'
 * or 'Contracts' folder for better organization (e.g., 'src/Alembic.Agent.Core/Models/AgentAction.cs').
 */
public enum AgentAction
{
    CreateFile,
    DotnetBuild,
    DotnetNewClasslib,
    GitCommit
}