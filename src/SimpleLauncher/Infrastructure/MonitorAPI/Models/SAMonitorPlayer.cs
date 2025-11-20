namespace SimpleLauncher.Infrastructure.MonitorAPI.Models
{
    public class SAMonitorPlayer
    {
        public int Id { get; set; }
        public int Ping { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
        public override bool Equals(object? obj)
        {
            if (obj is not SAMonitorPlayer other)
                return false;
            return Id == other.Id &&
               Name == other.Name;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }
}
