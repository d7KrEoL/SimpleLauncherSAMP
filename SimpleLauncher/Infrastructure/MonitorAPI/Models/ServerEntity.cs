namespace SimpleLauncher.Infrastructure.MonitorAPI.Models
{
    public class ServerEntity
    {
        public string Source { get; init; }
        public DateTime UpdateTime { get; init; }
        public string Name { get; init; }
        public string UriAddress { get; init; }
        public string IpAddress { get; init; }
        public uint Ping { get; private set; }
        public string WebUrl { get; private set; }
        public string Language { get; private set; }
        public string Gamemode { get; private set; }
        public string Version { get; private set; }
        public uint PlayersCount { get; private set; }
        public uint MaxPlaers { get; private set; }
        public List<string> Players { get; private set; }
        public bool IsLagcomp { get; private set; } = false;
        public bool IsSampCac { get; private set; } = false;
        public bool IsOpenMp { get; private set; } = false;
        public bool HasPassword { get; private set; } = false;
        public ServerEntity(string source,
            DateTime updateTime,
            string name,
            string uriAddress,
            string ipAddress,
            uint ping,
            string webUrl,
            string language,
            string gamemode,
            string version,
            uint playersCount,
            uint maxPlayers,
            List<string> players,
            bool isLagcomp,
            bool isSampCac,
            bool isOpenMp,
            bool hasPassword)
        {
            Source = source;
            UpdateTime = updateTime;
            Name = name;
            UriAddress = uriAddress;
            IpAddress = ipAddress;
            Ping = ping;
            WebUrl = webUrl;
            Language = language;
            Gamemode = gamemode;
            Version = version;
            PlayersCount = playersCount;
            MaxPlaers = maxPlayers;
            Players = players;
            IsLagcomp = isLagcomp;
            IsSampCac = isSampCac;
            IsOpenMp = isOpenMp;
            HasPassword = hasPassword;
        }
        public static ServerEntity CreateEmpty()
            => new ServerEntity(string.Empty,
                DateTime.MinValue,
                string.Empty,
                string.Empty,
                string.Empty,
                uint.MaxValue,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                uint.MinValue,
                uint.MinValue,
                new List<string>(),
                false,
                false,
                false,
                false);
    }
}
