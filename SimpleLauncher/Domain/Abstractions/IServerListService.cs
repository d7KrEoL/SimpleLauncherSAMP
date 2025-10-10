using SimpleLauncher.Domain.Models;

namespace SimpleLauncher.Domain.Abstractions
{
    public interface IServerListService
    {
        Task UpdateMonitoringGatewayAsync(IMonitoringApiGateway monitoringApiGateway);
        Task<List<ServerMeta>?> GetServersAsync(CancellationToken cancellationToken);
        Task<ServerMeta?> GetServerInfoAsync(string ipAddress, 
            ushort port, 
            CancellationToken cancellationToken);
        Task<ServerMeta?> GetServerInfoAsync(string ipAddressAndPort,
            CancellationToken cancellationToken);
        Task<ServerMeta?> UpdateServerInfoAsync(string ipAddress, 
            ushort port, 
            CancellationToken cancellationToken);
        Task<List<PlayerMeta>?> GetServerPlayersAsync(string ipAddress, 
            ushort port,
            CancellationToken cancellationToken);
        Task<List<PlayerMeta>?> GetServerPlayersAsync(string ipAddressAndPort,
            CancellationToken cancellationToken);
    }
}