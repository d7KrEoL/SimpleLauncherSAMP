using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Models;
using System.Diagnostics;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Infrastructure.Game.Utils;

namespace SimpleLauncher.Infrastructure.Game
{
    public class GameProcessManager : IGameProcessManager
    {
        private const string ClientInjectionErrorMessage = "Cannot inject client process into the game";
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
            if (game.process is null)
                return game;
            if (!await StartClientProcessAsync(game.process,
                clientExecutablePath,
                serverIpAndPort,
                cancellationToken))
            {
                _logger.LogError(ClientInjectionErrorMessage);
                return (null, ClientInjectionErrorMessage);
            }
            foreach (var addon in addons)
            {
                await _injectionService.InjectLibraryAsync(game.process,
                    addon.Path,
                    cancellationToken);
            }
            return (game.process, string.Empty);
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
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                _activeGameProcess = Process.Start(startInfo);
                if (_activeGameProcess is null)
                {
                    return (null, "Failed to start the game process.");
                }
                _logger.LogInformation("Game process started with PID: {Pid}", _activeGameProcess.Id);
                return (_activeGameProcess, string.Empty);
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
