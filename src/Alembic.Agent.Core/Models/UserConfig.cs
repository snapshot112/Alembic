namespace Alembic.Agent.Core.Models;

/*
 * Represents the user's security configuration, deserialized from settings.json.
 */
public class UserConfig
{
    // The key in the JSON is "VerificationSettings", which maps to this property.
    public Dictionary<SecurityLevel, VerificationMethod> VerificationSettings { get; set; } = new();
}