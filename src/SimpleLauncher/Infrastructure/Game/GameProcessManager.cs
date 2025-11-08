using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using SimpleLauncher.Infrastructure.Game.Utils;
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
        private readonly IProcessInjectionService _injectionService;
        private Process? _activeGameProcess;
        public GameProcessManager(ILogger<GameProcessManager> logger,
            IProcessInjectionService injectionService)
        {
            _logger = logger;
            _injectionService = injectionService;
        }
        public enum ActiveGameStatus
        {
            NotRunning,
            Running,
            Paused,
            Exited
        }
        public async Task<(Process? gameProcess, string errorMessage)> StartAndConnectAsync(string gameExecutablePath,
            string clientExecutablePath,
            string additionalArguments,
            string serverIpAndPort,
            List<GameAddon> addons,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            cancellationToken.Register(async () =>
            {
                if (_activeGameProcess is not null && !_activeGameProcess.HasExited)
                {
                    _logger.LogInformation("Cancellation requested. Terminating game process with PID: {Pid}", 
                        _activeGameProcess.Id);
                    await GameProcess.TerminateProcessAsync(_activeGameProcess);
                }
            });
            var game = await StartGameProcessAsync(gameExecutablePath, additionalArguments);
            if (!string.IsNullOrWhiteSpace(game.errorMessage) || game.process is null)
            {
                _logger.LogError("Failed to start game process: {ErrorMessage}", 
                    game.errorMessage);
                return game;
            }
            if (!await StartClientProcessAsync(game.process,
                clientExecutablePath,
                serverIpAndPort,
                cancellationToken))
            {
                _logger.LogError(ClientInjectionErrorMessage);
                await GameProcess.TerminateProcessAsync(game.process);
                return (null, ClientInjectionErrorMessage);
            }
            foreach (var addon in addons)
            {
                await _injectionService.InjectLibraryAsync(game.process,
                    addon.Path,
                    cancellationToken);
            }
            _activeGameProcess = game.process;
            GameProcess.ResumeProcess(game.process);
            
             return (game.process, string.Empty);
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
        private async Task<(Process? process, string errorMessage)> StartGameProcessAsync(string gamePath, 
            string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = gamePath,
                    Arguments = arguments,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    Verb = "runas",
                    WorkingDirectory = Path.GetDirectoryName(gamePath)
                };
                var process = GameProcess.StartProcessSuspended(startInfo);
                if (process is null)
                {
                    return (null, "Failed to start the game process.");
                }
                _logger.LogInformation("Game process started with PID: {Pid}", process.Id);
                
                return (process, string.Empty);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // Пользователь отменил UAC запрос
                return (null, "Запрос прав администратора был отклонен пользователем");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting game process.");
                return (null, $"Error starting game process: {ex.Message}");
            }
        }

        private async Task<bool> StartClientProcessAsync(Process gameProcess,
            string clientPath,
            string connectIpAndPort,
            CancellationToken cancellationToken)
        {
            if (!await _injectionService.InjectLibraryAsync(gameProcess,
                clientPath,
                cancellationToken))
            {
                _logger.LogError(ClientInjectionErrorMessage);
                return false;
            }
            _logger.LogInformation("Client process injected successfully into game process with PID: {Pid}", 
                gameProcess.Id);
            return true;
        }
        
    }
}
