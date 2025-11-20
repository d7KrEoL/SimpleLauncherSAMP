using SimpleLauncher.Domain.Models;
using System.IO;

namespace SimpleLauncher.Application.Extensions
{
    public static class GameClientExtensions
    {
        public static bool IsEmpty(this GameClient client)
            => string.IsNullOrWhiteSpace(client.Name) ||
            string.IsNullOrWhiteSpace(client.Version) ||
            string.IsNullOrWhiteSpace(client.Path);
        public static bool IsBadPath(this GameClient client)
            => !Directory.Exists(client.Path) ||
            !Infrastructure.Game.Utils.GameFiles.IsGameDirectory(client.Path);
    }
}
