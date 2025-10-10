using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using System.Net;

namespace SimpleLauncher.Services
{
    public class ServerListService : IServerListService
    {
        public const ushort MinPortValue = 1000;
        public const ushort MaxPortValue = 9999;
        private readonly ILogger<ServerListService> _logger;
        private readonly ISampQueryAdapter _sampQueryAdapter;
        private IMonitoringApiGateway _monitoringApiGateway;
        public ServerListService(ILogger<ServerListService> logger,
            IMonitoringApiGateway monitoringApiGateway,
            ISampQueryAdapter sampQueryAdapter) 
        { 
            _logger = logger;
            _monitoringApiGateway = monitoringApiGateway;
            _sampQueryAdapter = sampQueryAdapter;
        }
        public async Task UpdateMonitoringGatewayAsync(IMonitoringApiGateway monitoringApiGateway)
        {
            _monitoringApiGateway = monitoringApiGateway;
            await Task.CompletedTask;
        }
        public async Task<List<ServerMeta>?> GetServersAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var servers = await _monitoringApiGateway.GetServers(cancellationToken);
            if (servers is null)
                return servers;
            return servers;
        }
        public async Task<ServerMeta?> GetServerInfoAsync(string ipAddress,
            ushort port,
            CancellationToken cancellationToken)
        {
            if (!IpAndPortValidator(ipAddress, port))
                return null;
            return await GetServerInfoAsync($"{ipAddress}:{port}", cancellationToken);
        }
        public async Task<ServerMeta?> GetServerInfoAsync(string ipAddressAndPort,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _sampQueryAdapter.GetServerInfoAsync(ipAddressAndPort, cancellationToken);
        }
        public async Task<ServerMeta?> UpdateServerInfoAsync(string ipAddress, 
            ushort port, 
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IpAndPortValidator(ipAddress, port))
                return null;
            return await _sampQueryAdapter.GetFullServerInfoAsync(ipAddress, port, cancellationToken);
        }
        public async Task<List<PlayerMeta>?> GetServerPlayersAsync(string ipAddressAndPort, 
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _sampQueryAdapter.GetServerPlayersAsync(ipAddressAndPort, cancellationToken);
        }
        public async Task<List<PlayerMeta>?> GetServerPlayersAsync(string ipAddress, 
            ushort port, 
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IpAndPortValidator(ipAddress, port))
                return null;
            return await GetServerPlayersAsync($"{ipAddress}:{port}", cancellationToken);
        }
        private static bool IpAndPortValidator(string ipAddress, 
            ushort port)
        {
            if (string.IsNullOrEmpty(ipAddress) ||
                port < MinPortValue ||
                port > MaxPortValue)
                return false;
            return true;
        }
    }
}
