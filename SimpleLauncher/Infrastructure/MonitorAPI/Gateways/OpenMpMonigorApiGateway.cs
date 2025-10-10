using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using SimpleLauncher.Infrastructure.MonitorAPI.Contracts.OpenMP;
using System.Net.Http;
using System.Text.Json;

namespace SimpleLauncher.Infrastructure.MonitorAPI.Gateways
{
    public class OpenMpMonigorApiGateway : IMonitoringApiGateway
    {
        private const string GatewayName = "OpenMpMonitor";

        public string Name { get; } = GatewayName;
        private readonly Uri? _uri;
        private readonly HttpClient? _httpClient;
        private readonly ILogger<OpenMpMonigorApiGateway> _logger;
        
        public OpenMpMonigorApiGateway(HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenMpMonigorApiGateway> logger)
        {
            _logger = logger;
            var options = new UriCreationOptions();
            options.DangerousDisablePathAndQueryCanonicalization = false;
            if (!Uri.TryCreate(configuration["MonitoringSources:OpenMP"],
                UriKind.Absolute,
                out _uri))
            {
                _logger.LogError("Cannot parse configuration url: {URL}", configuration["MonitoringSources:SAMonitor"]);
                return;
            }
            _httpClient = httpClient;
        }
        public async Task<List<ServerMeta>?> GetServers(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_httpClient is null || _uri is null)
                return null;
            const string ServerDetailedInfoEndpoint = "servers";
            var request = $"{_uri.ToString()}{ServerDetailedInfoEndpoint}";
            var requestResult = await _httpClient.GetAsync(request);
            if (requestResult is null)
                return null;
            var content = await requestResult.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<List<GetServersResponse>>(content,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            _logger.LogTrace("OpenMP server list response approved");
            return response?.Select(request => new ServerMeta(request.hn,
                request.ip,
                request.ip,
                0,
                string.Empty,
                request.la,
                request.gm,
                request.vn,
                request.pc,
                request.pm,
                new List<string>(),
                request.pa,
                false,
                request.omp,
                request.pr))
                .ToList();
        }
        public async Task<ServerMeta?> GetServerInfo(string serverIp,
            ushort serverPort,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotImplementedException();
        }
        public async Task<List<PlayerMeta>?> GetServerPlayers(string serverIp,
            ushort serverPort,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotImplementedException();
        }
    }
}
