using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Infrastructure.MonitorAPI.Contracts.SAMonitor;
using SimpleLauncher.Infrastructure.MonitorAPI.Models;
using SimpleLauncher.Infrastructure.MonitorAPI.Mappers;
using SimpleLauncher.Infrastructure.MonitorAPI.Utils;
using System.Net.Http;
using System.Text.Json;
using SimpleLauncher.Domain.Models;

namespace SimpleLauncher.Infrastructure.MonitorAPI.Gateways
{
    /// <summary>
    /// SAMonitor API documentation can be found here:
    /// https://github.com/markski1/SAMonitor?tab=readme-ov-file#masterlist
    /// </summary>
    class SAMonitorApiGateway : IMonitoringApiGateway
    {
        private const string GatewayName = "SAMonitor";

        public string Name { get; } = GatewayName;
        private readonly Uri? _uri;
        private readonly HttpClient? _httpClient;
        private readonly ILogger<SAMonitorApiGateway> _logger;
        
        public SAMonitorApiGateway(HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SAMonitorApiGateway> logger)
        {
            _logger = logger;
            var options = new UriCreationOptions();
            options.DangerousDisablePathAndQueryCanonicalization = false;
            if (!Uri.TryCreate(configuration["MonitoringSources:SAMonitor"],
                UriKind.Absolute,
                out _uri))
            {
                _logger.LogError("Cannot parse configuration url: {URL}", configuration["MonitoringSources:SAMonitor"]);
                return;
            }
            _httpClient = httpClient;
        }
        public string GetName() => GatewayName;
        public async Task<List<ServerMeta>?> GetServers(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_httpClient is null || _uri is null)
                return null;
            const string GetServersEndpoint = "GetAllServers";
            var requestResult = await _httpClient.GetAsync($"{_uri}/{GetServersEndpoint}");
            if (requestResult is null)
                return null;
            cancellationToken.ThrowIfCancellationRequested();
            var content = await requestResult.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<List<GetServersResponse>>(content,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })?.ToList();
            var entities = response?
                .Select(entity =>
                    new ServerEntity(
                        nameof(SAMonitorApiGateway),
                        entity.lastUpdated,
                        entity.name,
                        entity.ipAddr,
                        entity.ipAddr,
                        0,
                        entity.website,
                        entity.language,
                        entity.gameMode,
                        entity.version,
                        entity.playersOnline,
                        entity.maxPlayers,
                        new List<string>(),
                        entity.lagComp,
                        !entity.sampCac.Equals("Not required"),
                        entity.isOpenMp,
                        entity.requiresPassword
                        )
                    .ToApplicationModel())
                .ToList();
            _logger.LogDebug("Server list response approved");
            cancellationToken.ThrowIfCancellationRequested();
            return entities;
        }
        public async Task<ServerMeta?> GetServerInfo(string serverIp, 
            ushort serverPort,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_httpClient is null || _uri is null)
                return null;
            const string ServerDetailedInfoEndpoint = "GetServerByIP";
            var request = UriUtils.BuildUrlWithQuery($"{_uri.ToString()}/{ServerDetailedInfoEndpoint}", 
                new GetServerByIpRequest($"{serverIp}:{serverPort}"));
            var requestResult = await _httpClient.GetAsync(request);
            if (requestResult is null)
                return null;
            var content = await requestResult.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<GetServerByIpResponse>(content, 
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            return new ServerMeta(response.name,
                response.ipAddr,
                response.ipAddr,
                0,
                response.website,
                response.language,
                response.gameMode,
                response.version,
                response.playersOnline,
                response.maxPlayers,
                new List<string>(),
                response.lagComp,
                response.sampCac.Equals("Not required"),
                response.isOpenMp,
                response.requiresPassword);
        }
        /// <summary>
        /// This method fetches the list of players currently on the specified server. (SAMonitor API)
        /// </summary>
        /// <remarks>
        /// This method is not currently (31.10.2025) working because SAMonitor service does not monitor list of players anymore.
        /// </remarks>
        /// <param name="serverIp"></param>
        /// <param name="serverPort"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>List of PlayerMeta or null if there is no players</returns>
        public async Task<List<PlayerMeta>?> GetServerPlayers(string serverIp, 
            ushort serverPort,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_httpClient is null || _uri is null)
                return null;
            const string ServerPlayersListEndpoint = "GetServerPlayers";
            var request = Utils.UriUtils.BuildUrlWithQuery($"{_uri.ToString()}/{ServerPlayersListEndpoint}",
                new GetServerPlayersRequest(serverIp, serverPort.ToString()));
            var requestResult = await _httpClient.GetAsync(request);
            if (requestResult is null)
                return null;
            var content = await requestResult.Content.ReadAsStringAsync();
            try
            {
                var response = JsonSerializer.Deserialize<GetServerPlayersResponse>(content,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    })?
                    .players
                    .Select(player =>
                        new PlayerMeta(player.Id, player.Name, player.Score, player.Ping))
                    .ToList();
                return response;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing server players response: {Content}", content);
                return null;
            }
        }
    }
}
