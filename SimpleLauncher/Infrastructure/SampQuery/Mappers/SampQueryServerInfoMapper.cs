using SimpleLauncher.Domain.Models;
using SimpleLauncher.Infrastructure.SampQuery.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SimpleLauncher.Infrastructure.SampQuery.Mappers
{
    public static class SampQueryServerInfoMapper
    {
        public static ServerMeta ToApplicationModel(this QueryServerInfo queryServerInfo, string serverIp, ushort port)
            => new ServerMeta(queryServerInfo.HostName,
                serverIp,
                serverIp,
                (uint)queryServerInfo.ServerPing,
                string.Empty,
                queryServerInfo.Language,
                queryServerInfo.GameMode,
                string.Empty,
                queryServerInfo.Players,
                queryServerInfo.MaxPlayers,
                new List<string>(),
                false,
                false,
                false,
                queryServerInfo.Password);
        public static ServerMeta ToApplicationModel(this QueryServerInfo queryServerInfo, 
            string serverIp, 
            ushort port,
            QueryServerRules rules,
            List<string>? players)
            => new ServerMeta(queryServerInfo.HostName,
                serverIp,
                serverIp,
                (uint)queryServerInfo.ServerPing,
                rules.WebUrl?.ToString() ?? string.Empty,
                queryServerInfo.Language,
                queryServerInfo.GameMode,
                rules.Version ?? string.Empty,
                queryServerInfo.Players,
                queryServerInfo.MaxPlayers,
                players ?? new List<string>(),
                rules.LagComp,
                rules.SampcacVersion is not null,
                false,
                queryServerInfo.Password);
    }
}
