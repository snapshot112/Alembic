using System.Text.Json.Serialization;

namespace Alembic.Agent.Core.Models;

/*
 * Defines the possible verification methods a user can require.
 */
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VerificationMethod
{
    None,
    Password,
    Authenticator
}