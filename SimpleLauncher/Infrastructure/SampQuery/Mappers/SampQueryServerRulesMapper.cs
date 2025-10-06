using SimpleLauncher.Domain.Models;
using SimpleLauncher.Infrastructure.SampQuery.Models;

namespace SimpleLauncher.Infrastructure.SampQuery.Mappers
{
    public static class SampQueryServerRulesMapper
    {
        public static ServerMeta ToApplicationModel(this QueryServerRules queryServerRules)
            => new ServerMeta(string.Empty,
                string.Empty,
                string.Empty,
                0,
                queryServerRules.WebUrl?.ToString() ?? string.Empty,
                string.Empty,
                string.Empty,
                queryServerRules.Version ?? string.Empty,
                0,
                0,
                new List<string>(),
                queryServerRules.LagComp,
                !string.IsNullOrEmpty(queryServerRules.SampcacVersion),
                false,
                false);
        public static ServerMeta ToApplicationModel(this QueryServerRules queryServerRules, QueryServerInfo serverInfo)
            => serverInfo.ToApplicationModel(string.Empty, 0, queryServerRules, null);
        public static ServerMeta ToApplicationModel(this QueryServerRules queryServerRules, string ip, ushort port, QueryServerInfo serverInfo)
            => serverInfo.ToApplicationModel(ip, port, queryServerRules, null);
    }
}
