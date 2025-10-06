using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using SimpleLauncher.Infrastructure.MonitorAPI.Models;

namespace SimpleLauncher.Infrastructure.MonitorAPI.Gateways
{
    /// <summary>
    /// G4su API documentation can be found here:
    /// https://github.com/gs4u/api_doc/blob/master/README_RU.md
    /// </summary>
    class GS4uApiGateway /*: IMonitoringApiGateway*/
    {
        public async Task<List<ServerMeta>> GetServers()
        {
            var servers = new List<ServerMeta>();
            throw new NotImplementedException();
            return servers;
        }
    }
}
