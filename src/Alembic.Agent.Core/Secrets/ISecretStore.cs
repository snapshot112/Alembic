using System.Threading.Tasks;

namespace Alembic.Agent.Core.Secrets;

/*
 * Defines a contract for a secure storage provider for sensitive data
 * like API keys and tokens.
 */
public interface ISecretStore
{
    /*
     * Securely stores a secret value associated with a key.
     * If a secret for the given key already exists, it should be overwritten.
     * @param key The unique identifier for the secret (e.g., "GitHubApiToken").
     * @param secret The sensitive value to store.
     */
    Task StoreSecretAsync(string key, string secret);

    /*
     * Retrieves a secret value associated with a key.
     * @param key The unique identifier for the secret.
     * @returns The secret value, or null if the key is not found.
     */
    Task<string?> RetrieveSecretAsync(string key);
    
    /*
     * Deletes a secret value associated with a key.
     * @param key The unique identifier for the secret to delete.
     */
    Task DeleteSecretAsync(string key);
}
