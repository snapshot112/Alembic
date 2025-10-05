using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Alembic.Agent.Core.Helpers;
using Alembic.Agent.Core.Logging;

namespace Alembic.Agent.Core.Secrets;

#if WINDOWS
/*
 * A concrete implementation of ISecretStore that uses .NET's ProtectedData
 * to encrypt secrets to the local file system, scoped to the current user.
 * This implementation is only available on Windows.
 */
public class ProtectedDataSecretStore : ISecretStore
{
    private readonly string _storagePath;

    public ProtectedDataSecretStore()
    {
        // Create a hidden directory in the repository root for storing encrypted secrets.
        _storagePath = Path.Combine(ProjectEnvironment.RepositoryRoot, ".secrets");
        Directory.CreateDirectory(_storagePath);
    }

    public Task StoreSecretAsync(string key, string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        // Encrypt the data using the current user's credential scope.
        var encryptedBytes = ProtectedData.Protect(secretBytes, null, DataProtectionScope.CurrentUser);
        var filePath = GetFilePathForKey(key);
        // Resolved ambiguity by specifying the CancellationToken.
        return File.WriteAllBytesAsync(filePath, encryptedBytes, CancellationToken.None);
    }

    public async Task<string?> RetrieveSecretAsync(string key)
    {
        var filePath = GetFilePathForKey(key);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var encryptedBytes = await File.ReadAllBytesAsync(filePath);
        try
        {
            // Decrypt the data using the current user's credential scope.
            var secretBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(secretBytes);
        }
        catch (CryptographicException)
        {
            // This can happen if the file is corrupted or was encrypted by another user.
            return null;
        }
    }

    public Task DeleteSecretAsync(string key)
    {
        var filePath = GetFilePathForKey(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    private string GetFilePathForKey(string key)
    {
        // Sanitize the key to make it a safe file name by Base64 encoding it.
        var safeFileName = Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
            
        return Path.Combine(_storagePath, safeFileName);
    }
}
#else
/*
 * [WARNING: INSECURE] A development-only implementation for non-Windows platforms.
 * This class stores secrets in PLAIN TEXT and is NOT suitable for production.
 * It exists only to unblock development on Linux and macOS.
 */
public class ProtectedDataSecretStore : ISecretStore
{
    private readonly string _storagePath;

    public ProtectedDataSecretStore()
    {
        _storagePath = Path.Combine(ProjectEnvironment.RepositoryRoot, ".secrets_dev_insecure");
        Directory.CreateDirectory(_storagePath);
    }

    public Task StoreSecretAsync(string key, string secret)
    {
        var filePath = GetFilePathForKey(key);
        return File.WriteAllTextAsync(filePath, secret, CancellationToken.None);
    }

    public async Task<string?> RetrieveSecretAsync(string key)
    {
        var filePath = GetFilePathForKey(key);
        if (!File.Exists(filePath))
        {
            return null;
        }

        return await File.ReadAllTextAsync(filePath);
    }

    public Task DeleteSecretAsync(string key)
    {
        var filePath = GetFilePathForKey(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    private string GetFilePathForKey(string key)
    {
        var safeFileName = Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
            
        return Path.Combine(_storagePath, safeFileName);
    }
}
#endif
