using SimpleLauncher.Domain.Models;

namespace SimpleLauncher.Domain.Abstractions
{
    public interface IMonitoringApiGateway
    {
        string Name { get; }
        Task<List<ServerMeta>?> GetServers(CancellationToken cancellationToken);
        Task<ServerMeta?> GetServerInfo(string serverIp, 
            ushort serverPort, 
            CancellationToken cancellationToken);
        Task<List<PlayerMeta>?> GetServerPlayers(string serverIp, 
            ushort serverPort, 
            CancellationToken cancellationToken);
    }
}
