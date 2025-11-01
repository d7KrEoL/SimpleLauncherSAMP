using SimpleLauncher.Domain.Models;

namespace SimpleLauncher.Domain.Abstractions
{
    public interface IServerListService
    {
        /// <summary>
        /// Updates currently using gateway for monitoring API.
        /// </summary>
        /// <param name="monitoringApiGateway">
        /// Gateway instance that will be used to fetch data from remote monitor service</param>
        /// <returns></returns>
        Task UpdateMonitoringGatewayAsync(IMonitoringApiGateway monitoringApiGateway);
        /// <summary>
        /// Asynchronously retrieves a list of metadata for available servers.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of servers with 
        /// <see cref="ServerMeta"/> information or <see langword="null"/> if no servers received
        /// </returns>
        Task<List<ServerMeta>?> GetServersAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Asynchronously retrieves metadata information about a server at the specified IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address of the server to query. Must be a valid IPv4 or IPv6 address.</param>
        /// <param name="port">
        /// The network port number on which the server is listening. Must be within the range 1000 to 9999.
        /// Sometimes port can be a bigger value upto 65535.
        /// </param>
        /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="ServerMeta"/>
        /// object with server metadata if the server is reachable; otherwise, <see langword="null"/>.
        /// </returns>
        Task<ServerMeta?> GetServerInfoAsync(string ipAddress, 
            ushort port, 
            CancellationToken cancellationToken);
        /// <summary>
        /// Asynchronously retrieves metadata information about a server at the specified IP address and port.
        /// </summary>
        /// <param name="ipAddressAndPort">
        /// The IP address and port of the target server, formatted as "address:port". Must not be null or empty.
        /// Address must be a valid IPv4 or IPv6 address, and port must be an integer between 1000 and 9999.
        /// Sometimes port can be a bigger value upto 65535.
        /// </param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="ServerMeta"/>
        /// object with the server's metadata if successful; otherwise, <see langword="null"/> if the server information
        /// could not be retrieved.</returns>
        Task<ServerMeta?> GetServerInfoAsync(string ipAddressAndPort,
            CancellationToken cancellationToken);
        /// <summary>
        /// Asynchronously updates the server metadata for the specified IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IPv4 or IPv6 address of the server whose metadata is to be updated. Cannot be null or empty.</param>
        /// <param name="port">
        /// The network port number of the server. Must be a valid port in the range 1000 to 9999.
        /// Sometimes port can be a bigger value upto 65535.
        /// </param>
        /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="ServerMeta"/>
        /// object with the updated server metadata, or <see langword="null"/> if the server information could not be
        /// retrieved.
        /// </returns>
        Task<ServerMeta?> UpdateServerInfoAsync(string ipAddress, 
            ushort port, 
            CancellationToken cancellationToken);
        /// <summary>
        /// Asynchronously retrieves metadata for all players currently connected to the specified game server.
        /// </summary>
        /// <param name="ipAddress">
        /// The IPv4 or IPv6 address of the game server from which to retrieve player information. Cannot be null or
        /// empty.</param>
        /// <param name="port">
        /// The network port number on which the game server is listening. Must be a valid port in the range 1000 to 9999.
        /// Sometimes port can be a bigger value upto 65535.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of <see cref="PlayerMeta"/> for
        /// all connected players, or <see langword="null"/> if the server is unreachable or no data is available.
        /// </returns>
        Task<List<PlayerMeta>?> GetServerPlayersAsync(string ipAddress, 
            ushort port,
            CancellationToken cancellationToken);
        /// <summary>
        /// Asynchronously retrieves metadata for all players currently connected to the specified game server.
        /// </summary>
        /// <param name="ipAddressAndPort">
        /// The IP address and port of the target server, formatted as "address:port". Must not be null or empty.
        /// Address must be a valid IPv4 or IPv6 address, and port must be an integer between 1000 and 9999.
        /// Sometimes port can be a bigger value upto 65535.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of <see cref="PlayerMeta"/> for
        /// all connected players, or <see langword="null"/> if the server is unreachable or no data is available.
        /// </returns>
        Task<List<PlayerMeta>?> GetServerPlayersAsync(string ipAddressAndPort,
            CancellationToken cancellationToken);
        /// <summary>
        /// Asynchronously retrieves metadata for all players currently connected to the specified game server using
        /// monitoring data.
        /// </summary>
        /// <remarks>
        /// This function will not return data when working with some monitorings, like: SAMonitor, OpenMP monitor,
        /// because they do not provide player information via their monitoring APIs.
        /// </remarks>
        /// <param name="ipAddress">
        /// The IPv4 or IPv6 address of the game server from which to retrieve player information. Cannot be null or
        /// empty.</param>
        /// <param name="port">
        /// The network port number on which the game server is listening. 
        /// Must be an integer between 1000 and 9999.
        /// Sometimes port can be a bigger value upto 65535.
        /// </param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of player metadata 
        /// <see cref="PlayerMeta"/> for all connected players, or <see langword="null"/> if monitoring data is unavailable.
        /// </returns>
        Task<List<PlayerMeta>?> GetServerPlayersFromMonitoringAsync(string ipAddress,
            ushort port,
            CancellationToken cancellationToken);
        /// <summary>
        /// Asynchronously retrieves metadata for all players currently connected to the specified game server using
        /// monitoring data.
        /// </summary>
        /// <remarks>
        /// This function will not return data when working with some monitorings, like: SAMonitor, OpenMP monitor,
        /// because they do not provide player information via their monitoring APIs.
        /// </remarks>
        /// <param name="ipAddressAndPort">
        /// The IP address and port of the target server, formatted as "address:port". Must not be null or empty.
        /// Address must be a valid IPv4 or IPv6 address, and port must be an integer between 1000 and 9999.
        /// Sometimes port can be a bigger value upto 65535.
        /// </param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of player metadata 
        /// <see cref="PlayerMeta"/> for all connected players, or <see langword="null"/> if monitoring data is unavailable.
        /// </returns>
        Task<List<PlayerMeta>?> GetServerPlayersFromMonitoringAsync(string ipAddressAndPort,
            CancellationToken cancellationToken);
    }
}