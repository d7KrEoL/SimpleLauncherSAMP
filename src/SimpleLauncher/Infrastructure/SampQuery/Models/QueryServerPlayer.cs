using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLauncher.Infrastructure.SampQuery.Models
{
    public class QueryServerPlayer
    {
        public byte PlayerId { get; init; }
        public string PlayerName { get; init; } = "UNKNOWN";
        public int PlayerScore { get; init; }
        public int PlayerPing { get; init; }
    }
}
