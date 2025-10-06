using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLauncher.Infrastructure.SampQuery.Models
{
    public class QueryServerInfo
    {
        public string HostName { get; init; } = "UNKNOWN";
        public string GameMode { get; init; } = "UNKNOWN";
        public string Language { get; init; } = "UNKNOWN";
        public ushort Players { get; init; }
        public ushort MaxPlayers { get; init; }
        public bool Password { get; init; }
        public int ServerPing { get; set; } = -1;
    }
}
