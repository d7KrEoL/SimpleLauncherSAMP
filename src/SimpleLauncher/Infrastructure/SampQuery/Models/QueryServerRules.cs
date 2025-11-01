namespace SimpleLauncher.Infrastructure.SampQuery.Models
{
    public class QueryServerRules
    {
        public bool LagComp { get; set; }
        public string? MapName { get; set; }
        public string? Version { get; set; }
        public string? SampcacVersion { get; set; }
        public int Weather { get; set; }
        public Uri? WebUrl { get; set; }
        public DateTime WorldTime { get; set; }
        public decimal Gravity { get; set; } = 0.008000M;
    }
}
