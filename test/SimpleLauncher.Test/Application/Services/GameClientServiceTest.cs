using Bogus;
using Microsoft.Extensions.Logging;
using Moq;
using SimpleLauncher.Application.Services;
using SimpleLauncher.Domain.Models;

namespace SimpleLauncher.Test.Application.Services
{
    public class GameClientServiceTest
    {
        private const string ClientName1 = "One";
        private const string ClientVersion1 = "1.0.0";
        private const string ClientPathUnexisting = "C:\\Path\\to\\file";
        private const string _clientLibraryName = "samp.dll";
        private const string _gameExecutableName = "gta_sa.exe";
        private readonly Mock<ILogger<GameClientService>> _loggerMock;
        private readonly string _rootDirectoryPath;

        public GameClientServiceTest()
        {
            _loggerMock = new Mock<ILogger<GameClientService>>();
            var path = SimpleLauncher.
                Infrastructure
                .Game
                .Utils
                .SystemRegistry
                .FindGamePathInRegistry();
            if (File.Exists(Path.GetFileName(path)))
                path = Path.GetDirectoryName(path);
            if (path is null)
                Assert.Fail("GameClientService cannot be tested without installed gta+samp on this system");
            _rootDirectoryPath = Path.GetDirectoryName(path) ?? path;
        }
        [Fact]
        public async Task AddGameClientsAsync_Unit0_ShouldReturn_Clients()
        {
            var service = new GameClientService(_loggerMock.Object);
            var result = await service
                                .AddGameClientAsync(new GameClient(ClientName1, 
                                ClientVersion1, 
                                _rootDirectoryPath,
                                _clientLibraryName,
                                _gameExecutableName));
            Assert.True(result);
            result = await service
                .AddGameClientAsync(new GameClient(ClientName1 + 'a',
                ClientVersion1,
                _rootDirectoryPath,
                _clientLibraryName,
                _gameExecutableName));
            Assert.True(result);
        }
        [Fact]
        public async Task AddGameClientsAsync_Unit1_RepeatingValues_ShouldReturn_False()
        {
            var service = new GameClientService(_loggerMock.Object);
            var result = await service
                                .AddGameClientAsync(new GameClient(ClientName1,
                                ClientVersion1,
                                _rootDirectoryPath,
                                _clientLibraryName,
                                _gameExecutableName));
            Assert.True(result);
            result = await service
                .AddGameClientAsync(new GameClient(ClientName1,
                ClientVersion1,
                _rootDirectoryPath,
                _clientLibraryName,
                _gameExecutableName));
            Assert.False(result);
        }
        [Fact]
        public async Task AddGameClientAsync_Unit2_NotExistingPath_ShouldReturn_True()
        {
            var service = new GameClientService(_loggerMock.Object);
            var result = await service
                                .AddGameClientAsync(new GameClient(ClientName1,
                                ClientVersion1,
                                ClientPathUnexisting,
                                ClientPathUnexisting,
                                ClientPathUnexisting));
            Assert.True(result);
        }
        [Fact]
        public async Task AddGameClientAsync_Unit3_NameIsEmpty_ShouldReturn_Null()
        {
            var service = new GameClientService(_loggerMock.Object);
            await Assert.ThrowsAsync<ArgumentException>(async () => await service
                                .AddGameClientAsync(string.Empty,
                                ClientVersion1,
                                ClientPathUnexisting));
        }
        [Fact]
        public async Task AddGameClientAsync_Unit4_VersionIsEmpty_ShouldReturn_Null()
        {
            var service = new GameClientService(_loggerMock.Object);
            await Assert.ThrowsAsync<DirectoryNotFoundException>(async () => await service
                                .AddGameClientAsync(ClientName1,
                                string.Empty,
                                ClientPathUnexisting));
        }
        [Fact]
        public async Task AddGameClientAsync_Unit5_PathIsEmpty_ShouldReturn_Null()
        {
            var service = new GameClientService(_loggerMock.Object);
            await Assert.ThrowsAsync<ArgumentException>(async () => await service
                                .AddGameClientAsync(ClientName1,
                                ClientVersion1,
                                string.Empty));
        }
        [Fact]
        public async Task AddGameClientAsync_Unit6_ShouldReturn_Client()
        {
            var service = new GameClientService(_loggerMock.Object);
            var result = await service
                                .AddGameClientAsync(ClientName1,
                                ClientVersion1,
                                _rootDirectoryPath);
            Assert.NotNull(result);
            Assert.Equal(ClientName1, result.Name);
            Assert.Equal(ClientVersion1, result.Version);
            Assert.Equal(_rootDirectoryPath, result.Path);
        }
        [Fact]
        public async Task AddGameClientsAsync_Unit7_PathWithFile_ShouldReturn_True()
        {
            var service = new GameClientService(_loggerMock.Object);
            var result = await service
                                .AddGameClientAsync(ClientName1,
                                ClientVersion1,
                                _rootDirectoryPath + "\\gta_sa.exe");
            Assert.NotNull(result);
            Assert.Equal(result.Path, _rootDirectoryPath);
        }
        [Fact]
        public async Task GetGameClientAsync_Unit0_ShouldReturn_Values()
        {
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(555);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < gameClients.Count; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            var getGameClients = await service.GetGameClientsAsync();
            Assert.True(getGameClients.SequenceEqual(gameClients));
        }
        [Fact]
        public async Task RemoveGameClientAsync_Unit0_ShouldReturn_Null()
        {
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(10);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < gameClients.Count; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            var removeResult = await service.RemoveGameClientAsync(gameClients[5]);
            var clients = await service.GetGameClientsAsync();
            var client = clients
                .Where(client => client.Equals(gameClients[5]))
                .FirstOrDefault();
            Assert.True(client is null);
        }
        [Fact]
        public async Task RemoveGameClientAsync_Unit1_DoubleRemove_ShouldReturn_False()
        {
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(10);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < gameClients.Count; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            var removeResult = await service.RemoveGameClientAsync(gameClients[5]);
            Assert.True(removeResult);
            removeResult = await service.RemoveGameClientAsync(gameClients[5]);
            Assert.False(removeResult);
        }
        [Fact]
        public async Task RemoveGameClientAsync_Unit2_RemoveByName_ShouldReturn_True()
        {
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(10);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < gameClients.Count; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            var removeResult = await service.RemoveGameClientAsync(gameClients[5].Name);
            Assert.True(removeResult);
        }
        [Fact]
        public async Task RemoveGameClientAsync_Unit3_DoubleRemoveByName_ShouldReturn_False()
        {
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(10);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < gameClients.Count; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            var removeResult = await service.RemoveGameClientAsync(gameClients[5].Name);
            Assert.True(removeResult);
            removeResult = await service.RemoveGameClientAsync(gameClients[5]);
            Assert.False(removeResult);
        }
        [Fact]
        public async Task RemoveGameClientAsync_Unit4_DoubleRemoveByName_ShouldReturn_False()
        {
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(10);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < gameClients.Count; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            var removeResult = await service.RemoveGameClientAsync(gameClients[5].Name);
            Assert.True(removeResult);
            removeResult = await service.RemoveGameClientAsync(gameClients[5].Name);
            Assert.False(removeResult);
        }
        [Fact]
        public async Task RemoveGameClientAsync_Unit5_DoubleRemoveByName_ShouldReturn_False()
        {
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(10);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < gameClients.Count; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            var removeResult = await service.RemoveGameClientAsync(gameClients[5]);
            Assert.True(removeResult);
            removeResult = await service.RemoveGameClientAsync(gameClients[5].Name);
            Assert.False(removeResult);
        }
        [Fact]
        public async Task FindGameClientAsync_Unit0_ShouldReturn_Client()
        {
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(10);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < gameClients.Count; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            var client = await service.FindGameClientAsync(gameClients[5].Name);
            Assert.Equal(client, gameClients[5]);
        }
        [Fact]
        public async Task UpdateGameClientNameAsync_Unit0_ShouldReturn_Client()
        {
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(5);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < gameClients.Count; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            var clientName = gameClients[3].Name + "-upd";
            var client = await service
                .UpdateGameClientNameAsync(gameClients[3].Name, 
                $"{gameClients[3].Name}-upd");
            Assert.NotNull(client);
            Assert.Equal(client.Name, clientName);
        }
        [Fact]
        public async Task UpdateGameClientNameAsync_Unit1_NotExistingClient_ShouldReturn_Null()
        {
            const int GeneratingClientsCount = 7;
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(GeneratingClientsCount);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < GeneratingClientsCount - 1; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            var client = await service
                .UpdateGameClientNameAsync(gameClients[GeneratingClientsCount - 1].Name,
                $"{gameClients[3].Name}-upd");
            Assert.Null(client);
        }
        [Fact]
        public async Task UpdateGameClientPathAsync_Unit0_ShouldReturn_Client()
        {
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(5);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < gameClients.Count; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            if (string.IsNullOrWhiteSpace(_rootDirectoryPath))
            {
                Console.WriteLine("Game registry key was not found, " +
                    "UpdateGameClientPathAsync_Unit0 test skipped");
                return;
            }
            var client = await service
                .UpdateGameClientPathAsync(gameClients[3].Name,
                _rootDirectoryPath);
            Assert.NotNull(client);
            Assert.Equal(client.Path, _rootDirectoryPath);
        }
        [Fact]
        public async Task UpdateGameClientPathAsync_Unit1_NotExistingClient_ShouldReturn_Null()
        {
            const int GeneratingClientsCount = 7;
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(GeneratingClientsCount);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < GeneratingClientsCount - 1; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            var directory = Path.GetDirectoryName(_rootDirectoryPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                Console.WriteLine("Game registry key was not found, " +
                    "UpdateGameClientPathAsync_Unit1 test skipped");
                return;
            }
            var client = await service
                .UpdateGameClientPathAsync(gameClients[GeneratingClientsCount - 1].Name,
                directory);
            Assert.Null(client);
        }
        [Fact]
        public async Task UpdateGameClientPathAsync_Unit2_NotExistingPath_ShouldReturn_Null()
        {
            const int GeneratingClientsCount = 7;
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(GeneratingClientsCount);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < GeneratingClientsCount - 1; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            var client = await service
                .UpdateGameClientPathAsync(gameClients[GeneratingClientsCount - 1].Name,
                gameClients[GeneratingClientsCount - 1].Path);
            Assert.Null(client);
        }
        [Fact]
        public async Task UpdateGameClientVersionAsync_Unit0_ShouldReturn_Client()
        {
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(5);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < gameClients.Count; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            var versionValue = gameClients[3].Version + "-upd";
            var client = await service
                .UpdateGameClientVersionAsync(gameClients[3].Name,
                gameClients[3].Version + "-upd");
            Assert.NotNull(client);
            Assert.Equal(client.Version, versionValue);
        }
        [Fact]
        public async Task UpdateGameClientVersionAsync_Unit1_NotExistingUser_ShouldReturn_Null()
        {
            const int GeneratingClientsCount = 7;
            var gameClientFaker = new Faker<GameClient>()
                                .CustomInstantiator(f => new GameClient(
                                    name: $"Client_{f.UniqueIndex}",
                                    version: $"0.3.{f.Random.Number(1, 7)}-R{f.Random.Number(1, 5)}",
                                    path: $"C:\\SA-MP\\client_{f.UniqueIndex}",
                                    clientExecutableName: _clientLibraryName,
                                    gameExecutableName: _gameExecutableName
                                ));
            List<GameClient> gameClients = gameClientFaker.Generate(GeneratingClientsCount);
            var service = new GameClientService(_loggerMock.Object);
            for (int i = 0; i < GeneratingClientsCount - 1; i++)
            {
                var result = await service.AddGameClientAsync(gameClients[i]);
                Assert.True(result);
            }
            var versionValue = gameClients[3].Version + "-upd";
            var client = await service
                .UpdateGameClientVersionAsync(gameClients[GeneratingClientsCount - 1].Name,
                versionValue);
            Assert.Null(client);
        }
    }
}
