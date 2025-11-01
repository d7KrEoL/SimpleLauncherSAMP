using System.Diagnostics;

namespace SimpleLauncher.Domain.Models
{
    public class PlayerMeta
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public int Score { get; private set; }
        public int Ping { get; private set; }
        public PlayerMeta(int id, string name, int score, int ping)
        {
            Id = id;
            Name = name;
            Score = score;
            Ping = ping;
        }
        public static PlayerMeta CreateEmpty(string name = "")
        {
            return new PlayerMeta(0, name, 0, 0);
        }
    }
}
