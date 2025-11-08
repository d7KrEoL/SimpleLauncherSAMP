using System.Numerics;

namespace SimpleLauncher.Infrastructure.MonitorAPI.Models
{
    public class SAMonitorPlayer
    {
        public int Id { get; set; }
        public int Ping { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
    }
}
