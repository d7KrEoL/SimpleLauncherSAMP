using System.IO;

namespace SimpleLauncher.Domain.Models
{
    public class GameClient
    {
        public string Name { get; private set; }
        public string Version { get; private set; }
        public string Path { get; private set; }
        public string ClientExecutableName { get; private set; }
        public string GameExecutableName { get; private set; }
        public GameClient(string name, 
            string version, 
            string path,
            string clientExecutableName,
            string gameExecutableName)
        {
            Name = name;
            Version = version;
            Path = path;
            ClientExecutableName = clientExecutableName;
            GameExecutableName = gameExecutableName;
        }
        public override bool Equals(object? obj)
        {
            if (obj is not GameClient other)
                return false;
            return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) &&
                   Version.Equals(other.Version, StringComparison.OrdinalIgnoreCase) &&
                   Path.Equals(other.Path, 
                   System.StringComparison.OrdinalIgnoreCase);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Name.ToLowerInvariant(), 
                Version.ToLowerInvariant(), 
                Path.ToLowerInvariant());
        }
        public void SetName(string name)
            => Name = name;
        public void SetPath(string path)
            => Path = path;
        public void SetVersion(string version)
            => Version = version;
    }
}
