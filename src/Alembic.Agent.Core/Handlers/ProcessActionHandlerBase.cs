using System.Diagnostics;
using Alembic.Agent.Core.Logging;
using Alembic.Agent.Core.Models;

namespace Alembic.Agent.Core.Handlers;

/*
 * An abstract base class for action handlers that need to execute external processes.
 * It contains the generalized, secure process executor.
 */
public abstract class ProcessActionHandlerBase(string rootDirectory, ILogger logger)
{
    protected readonly string RootDirectory = rootDirectory;
    protected readonly ILogger Logger = logger;

    /*
     * A generalized, secure process executor. It runs a specified executable
     * from a whitelist with a list of arguments, always sandboxed to the project's root directory.
     * @param executable The whitelisted command to run.
     * @param arguments The list of arguments to pass to the executable.
     */
    protected async Task ExecuteProcessAsync(AllowedExecutable executable, params string[] arguments)
    {
        var executableName = executable.ToString().ToLowerInvariant();
        
        var processStartInfo = new ProcessStartInfo
        {
            FileName = executableName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = RootDirectory
        };

        foreach (var arg in arguments)
        {
            processStartInfo.ArgumentList.Add(arg);
        }

        using var process = new Process();
        process.StartInfo = processStartInfo;

        var commandForLog = $"{executableName} {string.Join(" ", arguments.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}";
        Logger.LogInfo($"Executing in '{RootDirectory}': {commandForLog}");
        
        process.Start();
        
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();

        if (!string.IsNullOrEmpty(output)) Logger.LogInfo("Output:\n" + output);
        if (!string.IsNullOrEmpty(error)) Logger.LogError("Error Output:\n" + error);
        
        Logger.LogInfo($"Command finished with exit code: {process.ExitCode}");
    }
}
