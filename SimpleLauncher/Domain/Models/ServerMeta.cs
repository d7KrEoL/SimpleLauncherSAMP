using SimpleLauncher.Infrastructure.MonitorAPI.Contracts.SAMonitor;

namespace SimpleLauncher.Domain.Models
{
    public class ServerMeta
    {
        public string Name { get; set; }
        public string UriAddress { get; init; }
        public string IpAddress { get; init; }
        public uint Ping { get; private set; }
        public string WebUrl { get; private set; }
        public string Language { get; private set; }
        public string Gamemode { get; private set; }
        public string Version { get; private set; }
        public uint PlayersCount { get; private set; }
        public uint MaxPlaeyers { get; private set; }
        public List<string> Players { get; private set; }
        public bool IsLagcomp { get; private set; } = false;
        public bool IsSampCac { get; private set; } = false;
        public bool IsOpenMp { get; private set; } = false;
        public bool HasPassword { get; private set; } = false;
        public ServerMeta(string name, 
            string uriAddress, 
            string ipAddress, 
            uint ping, 
            string webUrl, 
            string language, 
            string gamemode, 
            string version, 
            uint playersCount, 
            uint maxPlaeyers,
            List<string> players, 
            bool isLagcomp, 
            bool isSampCac,
            bool isOpenMp,
            bool hasPassword)
        {
            Name = name;
            UriAddress = uriAddress;
            IpAddress = ipAddress;
            Ping = ping;
            WebUrl = webUrl;
            Language = language;
            Gamemode = gamemode;
            Version = version;
            PlayersCount = playersCount;
            MaxPlaeyers = maxPlaeyers;
            Players = players;
            IsLagcomp = isLagcomp;
            IsSampCac = isSampCac;
            IsOpenMp = isOpenMp;
            HasPassword = hasPassword;
        }
        public static ServerMeta CreateUnknown(string? serverName, string uriAddress, string ipAddress)
            => new ServerMeta(serverName ?? "Unknown (server is not responding)", 
                uriAddress, 
                ipAddress, 
                0, 
                "N/A", 
                "N/A", 
                "N/A", 
                "N/A", 
                0, 
                0, 
                new List<string>(), 
                false, 
                false, 
                false,
                false);
        public void SetPlayers(List<string> players)
            => Players = players.Any() ? players : Players;
        public void AddPlayer(string playerName)
            => Players.Append(playerName);
        public void UpdatePing(uint ping)
            => Ping = ping >= 0 ? ping : Ping;
        public void SetName(string name)
            => Name = string.IsNullOrWhiteSpace(name) ? Name : name;
    }
}
