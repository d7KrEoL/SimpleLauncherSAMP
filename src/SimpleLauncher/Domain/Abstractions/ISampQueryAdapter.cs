using SimpleLauncher.Domain.Models;
using SimpleLauncher.Infrastructure.SampQuery.Models;
using System.Net;

namespace SimpleLauncher.Domain.Abstractions
{
    public interface ISampQueryAdapter
    {
        /// <summary>
        /// Gets only main server information, excluding rules and players list
        /// </summary>
        /// <param name="ipAddressAndPort"></param>
        /// <returns>ServerMeta without external info and players list</returns>
        Task<ServerMeta> GetServerInfoAsync(string ipAddressAndPort, CancellationToken cancellationToken);
        Task<ServerMeta> GetServerInfoAsync(string ipAddress, ushort port, CancellationToken cancellationToken);
        bool GetServerIsOmp(string ipAddress, ushort port, CancellationToken cancellationToken);
        /// <summary>
        /// Gets list of players with additional information about each
        /// </summary>
        /// <param name="ipAddressAndPort"></param>
        /// <returns>List of PlayerMeta</returns>
        Task<List<PlayerMeta>> GetServerPlayersAsync(string ipAddressAndPort, CancellationToken cancellationToken);
        Task<List<PlayerMeta>> GetServerPlayersAsync(string ipAddress, ushort port, CancellationToken cancellationToken);
        Task<ServerMeta> GetServerRulesAsync(string ipAddress, ushort port, CancellationToken cancellationToken);
        /// <summary>
        /// Gets all information about server, including main info, rules and players list
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <returns>Filled ServerMeta</returns>
        Task<ServerMeta> GetFullServerInfoAsync(string ipAddress, ushort port, CancellationToken cancellationToken);
    }
}