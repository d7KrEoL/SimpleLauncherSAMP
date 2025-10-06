using SimpleLauncher.Domain.Models;

namespace SimpleLauncher.Domain.Abstractions
{
    public interface IServerListService
    {
        Task<List<ServerMeta>?> GetServers(CancellationToken cancellationToken);
        Task<ServerMeta?> GetServerInfo(string ipAddress, 
            ushort port, 
            CancellationToken cancellationToken);
        Task<ServerMeta?> UpdateServerInfo(string ipAddress, 
            ushort port, 
            CancellationToken cancellationToken);
        Task<List<PlayerMeta>?> GetServerPlayers(string ipAddress, 
            ushort port,
            CancellationToken cancellationToken);
    }
}