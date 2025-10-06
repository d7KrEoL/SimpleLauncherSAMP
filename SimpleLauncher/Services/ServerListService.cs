using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;

namespace SimpleLauncher.Services
{
    public class ServerListService : IServerListService
    {
        public const ushort MinPortValue = 1000;
        public const ushort MaxPortValue = 9999;
        private readonly ILogger<ServerListService> _logger;
        private readonly IMonitoringApiGateway _monitoringApiGateway;
        private readonly ISampQueryAdapter _sampQueryAdapter;
        public ServerListService(ILogger<ServerListService> logger,
            IMonitoringApiGateway monitoringApiGateway,
            ISampQueryAdapter sampQueryAdapter) 
        { 
            _logger = logger;
            _monitoringApiGateway = monitoringApiGateway;
            _sampQueryAdapter = sampQueryAdapter;
        }
        public async Task<List<ServerMeta>?> GetServers(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var servers = await _monitoringApiGateway.GetServers(cancellationToken);
            if (servers is null)
                return servers;
            return servers;
        }
        public async Task<ServerMeta?> GetServerInfo(string ipAddress, 
            ushort port, 
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IpAndPortValidator(ipAddress, port))
                return null;
            return await _monitoringApiGateway.GetServerInfo(ipAddress, port, cancellationToken);
        }
        public async Task<ServerMeta?> UpdateServerInfo(string ipAddress, 
            ushort port, 
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IpAndPortValidator(ipAddress, port))
                return null;
            return await _sampQueryAdapter.GetFullServerInfoAsync(ipAddress, port, cancellationToken);
        }
        public async Task<List<PlayerMeta>?> GetServerPlayers(string ipAddress, 
            ushort port, 
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IpAndPortValidator(ipAddress, port))
                return null;
            return await _sampQueryAdapter.GetServerPlayersAsync(ipAddress, port, cancellationToken);
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
