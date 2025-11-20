using Microsoft.Extensions.Logging;
using SimpleLauncher.Application.Extensions;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using System.Collections.ObjectModel;
using System.IO;

namespace SimpleLauncher.Application.Services
{
    public class GameClientService : IGameClientService
    {
        private readonly ILogger<GameClientService> _logger;
        private ObservableCollection<GameClient> _clients = new();
        public ObservableCollection<GameClient> Clients => _clients;
        public GameClientService(ILogger<GameClientService> logger)
        {
            _logger = logger;
        }
        public async Task<ObservableCollection<GameClient>> GetGameClientsAsync()
            => _clients;
        public async Task<GameClient?> AddGameClientAsync(string name,
            string version,
            string path)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(nameof(name));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException(nameof(path));
            string? gameExecutable, clientExecutable;
            if (File.Exists(path))
                path = Path.GetDirectoryName(path) ??
                        throw new ArgumentException($"Wrong path (is targeting FileName) {path}",
                            nameof(path));
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"No such a directory: {path}");
            gameExecutable = Path.GetFileName(Infrastructure.Game.Utils.GameFiles
                .GetGameExecutableInDirectory(path));
            if (gameExecutable is null)
            {
                _logger.LogWarning("Cannot find game executable in directory: {path}", path);
                return null;
            }
            clientExecutable = Path.GetFileName(Infrastructure.Game.Utils.GameFiles
                .GetClientLibraryInDirectory(path));
            if (clientExecutable is null)
            {
                _logger.LogWarning("Cannot find client executable in directory: {path}", path);
                return null;
            }

            var client = new GameClient(name, 
                version, 
                path, 
                clientExecutable, 
                gameExecutable);
            if (client.IsBadPath())
            {
                _logger.LogWarning("Is not a game(samp) directory: {path}", path);
                return null;
            }
            var exactElement = FindExistingGameClient(client);
            if (exactElement is not null)
            {
                _logger.LogInformation("GameClient: ({name}) is already exist with name: {oldName}", 
                    name,
                    exactElement.Name);
                return exactElement;
            }
            _clients.Add(client);
            return client;
        }
        public async Task<bool> AddGameClientAsync(GameClient gameClient)
        {
            if (gameClient.IsEmpty())
            {
                _logger.LogError("Cannot add game client: {name} - client is empty", 
                    gameClient.Name);
                return false;
            }
            var client = FindExistingGameClient(gameClient);
            if (client is not null)
            {
                _logger.LogInformation("GameClient: ({name}) is already exist with name {oldName}",
                    gameClient.Name,
                    client.Name);
                return false;
            }
            _clients.Add(gameClient);
            return true;
        }
        public async Task<bool> RemoveGameClientAsync(GameClient gameClient)
        {
            var client = FindExistingGameClient(gameClient);
            if (client is null)
                return false;
            _clients.Remove(client);
            return true;
        }
        public async Task<bool> RemoveGameClientAsync(string clientName)
        {
            var client = FindExistingGameClient(clientName);
            if (client is null)
                return false;
            return _clients.Remove(client);
        }
        public async Task<GameClient?> UpdateGameClientNameAsync(string oldName, string newName)
        {
            var client = FindExistingGameClient(oldName);
            if (client is null)
                return null;
            client.SetName(newName);
            return client;
        }
        public async Task<GameClient?> UpdateGameClientPathAsync(string clientName, string path)
        {
            var client = FindExistingGameClient(clientName);
            if (client is null)
                return null;
            if (!Infrastructure.Game.Utils.GameFiles.IsGameDirectory(path))
                return null;
            client.SetPath(path);
            return client;
        }
        public async Task<GameClient?> UpdateGameClientVersionAsync(string clientName, string version)
        {
            var client = FindExistingGameClient(clientName);
            if (client is null)
                return null;
            client.SetVersion(version);
            return client;
        }
        public async Task<GameClient?> FindGameClientAsync(string clientName)
            => FindExistingGameClient(clientName);
        private GameClient? FindExistingGameClient(GameClient client)
            => _clients.Where(c => c.Equals(client))
                .FirstOrDefault();
        private GameClient? FindExistingGameClient(string clientName)
            => _clients.Where(c => c.Name.Equals(clientName))
            .FirstOrDefault();
    }
}
