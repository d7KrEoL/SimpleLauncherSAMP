using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using static SimpleLauncher.Domain.Abstractions.IGameProcessManager;

namespace SimpleLauncher.Infrastructure.Game
{
    public class GameProcessManager : IGameProcessManager
    {
        private const string ClientInjectionErrorMessage = "Cannot inject client process into the game";
        private const string SampInjectorExternalExecutablePath = "Injector\\samp-injector.exe";
        private const string InjectionTypeSamp = "samp";
        private const string InjectionTypeOmp = "omp";
        private readonly ILogger<GameProcessManager> _logger;
        public GameProcessManager(ILogger<GameProcessManager> logger)
        {
            _logger = logger;
        }
        public enum ActiveGameStatus
        {
            NotRunning,
            Running,
            Paused,
            Exited
        }
        public async Task<string> StartAndConnectAsync(GameLaunchInjectionType injectType,
            string gamePath,
            string ip,
            string port,
            string nick,
            string password,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(gamePath))
                return "Wrong game path";
            if (string.IsNullOrWhiteSpace(ip))
                return "Wrong ip address";
            if (string.IsNullOrWhiteSpace(port))
                return "Wrong server port";
            if (string.IsNullOrWhiteSpace(nick))
                return "Wrong nickname";
            _logger.LogInformation("Will launch path: {path}", gamePath);
            var arguments = $"\"{(injectType.Equals(GameLaunchInjectionType.SAMP) ?
                        InjectionTypeSamp :
                        InjectionTypeOmp)}\" " +
                    $"\"{gamePath}\" " +
                    $"\"{nick}\" " +
                    $"\"{ip}\" " +
                    $"\"{port}\"";
            if (!string.IsNullOrWhiteSpace(password))
                arguments += $" \"{password}\"";
                var processStartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.CurrentDirectory,
                    SampInjectorExternalExecutablePath),
                Arguments = arguments,
                WorkingDirectory = gamePath,
                Verb = "runas",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            try
            {
                _logger.LogInformation("Arguments: {args}", processStartInfo.Arguments);
                using (var process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    process.Start();

                    var outputTask = await process.StandardOutput.ReadToEndAsync();
                    var errorTask = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    if (!string.IsNullOrWhiteSpace(outputTask))
                        _logger.LogInformation("Game output log: {output}", outputTask);
                    if (!string.IsNullOrWhiteSpace(errorTask))
                        _logger.LogError("Game error log: {error}", errorTask);
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                return "UAC request was denied by user";
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error creating game process!");
                return ex.Message;
            }
            return string.Empty;
        }
        
    }
}
