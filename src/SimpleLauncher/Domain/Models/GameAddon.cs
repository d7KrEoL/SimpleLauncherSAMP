using Microsoft.Extensions.Logging;

namespace SimpleLauncher.Domain.Models
{
    public class GameAddon
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public AddonType Type { get; init; }
        public string Version { get; init; }
        public string Author { get; init; }
        public string InfoUrl { get; init; }
        public string Path { get; init; }
        public IEnumerable<string>? StartupArguments { get; init; }
        public IEnumerable<string>? StartupBatchCommands { get; init; }
        public GameAddon(string name, 
            string? description,
            AddonType type,
            string? version, 
            string? author, 
            string? downloadUrl,
            string path,
            IEnumerable<string>? startupArguments,
            IEnumerable<string>? startupBatchCommands)
        {
            Name = name;
            Description = description ?? string.Empty;
            Type = type;
            Version = version ?? string.Empty;
            Author = author ?? string.Empty;
            InfoUrl = downloadUrl ?? string.Empty;
            Path = path ?? string.Empty;
            StartupArguments = startupArguments;
            StartupArguments = startupBatchCommands;
        }
        public static GameAddon CreateEmpty(string name = "Unnamed")
        {
            return new GameAddon(name,
                                null,
                                AddonType.None,
                                null,
                                null,
                                null,
                                string.Empty,
                                null,
                                null);
        }
        public enum AddonType
        {
            None,
            Dll,
            Asi,
            LuaScript,
            CleoScript,
            ExecutableFile,
            BatchScript
        }
        public async Task InitializeAsync(IAddonContext context)
        {
            throw new NotImplementedException();
        }
        public async Task ShutdownAsync()
        {
            throw new NotImplementedException();
        }
        

        public interface IAddonContext
        {
            ILogger Logger { get; }
            IServiceProvider Services { get; }
            CancellationToken CancellationToken { get; }
        }
    }
    public class GameAddonContext
    {
        private readonly ILogger<GameAddonContext> _logger;
        private readonly IServiceProvider _services;
        private readonly CancellationToken _cancellationToken;
        public GameAddonContext(ILogger<GameAddonContext> logger,
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            _logger = logger;
            _services = services;
            _cancellationToken = cancellationToken;
        }
    }
}
