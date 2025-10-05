using System.Text.Json;
using Alembic.Agent.Core.Logging;
using Alembic.Agent.Core.Models;
using Alembic.Agent.Core.Secrets;

namespace Alembic.Agent.Core.Handlers;

/*
 * Handles all actions related to secure secret management.
 */
public class SecretActionHandler : IActionHandler
{
    private readonly ILogger _logger;
    private readonly ISecretStore _secretStore;

    public SecretActionHandler(ILogger logger, ISecretStore secretStore)
    {
        _logger = logger;
        _secretStore = secretStore;
    }

    public bool CanHandle(AgentAction action) =>
        action is AgentAction.StoreSecret or AgentAction.RetrieveSecret;

    public async Task HandleActionAsync(Command command)
    {
        switch (command.Action)
        {
            case AgentAction.StoreSecret:
                await HandleStoreSecretAsync(command.Parameters);
                break;
            case AgentAction.RetrieveSecret:
                await HandleRetrieveSecretAsync(command.Parameters);
                break;
            default:
                _logger.LogError($"SecretActionHandler cannot handle action '{command.Action}'.");
                break;
        }
    }

    private async Task HandleStoreSecretAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("key", out var keyProp) || keyProp.GetString() is not { } key ||
            !parameters.TryGetProperty("secret", out var secretProp) || secretProp.GetString() is not { } secret)
        {
            _logger.LogError("'key' and 'secret' parameters are required for StoreSecret action.");
            return;
        }

        await _secretStore.StoreSecretAsync(key, secret);
        _logger.LogInfo($"Secret for key '{key}' has been securely stored.");
    }

    private async Task HandleRetrieveSecretAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("key", out var keyProp) || keyProp.GetString() is not { } key)
        {
            _logger.LogError("'key' parameter is required for RetrieveSecret action.");
            return;
        }

        var secret = await _secretStore.RetrieveSecretAsync(key);
        if (secret is null)
        {
            _logger.LogWarning($"No secret found for key '{key}'.");
        }
        else
        {
            // In a real application, this would be passed to another command, not logged.
            // For now, we log a confirmation that it was retrieved.
            _logger.LogInfo($"Secret for key '{key}' was successfully retrieved.");
        }
    }
}
