using SimpleLauncher.Domain.Models;
using SimpleLauncher.Infrastructure.MonitorAPI.Models;

namespace SimpleLauncher.Infrastructure.MonitorAPI.Mappers
{
    public static class SAMonitorServerEntityMapper
    {
        public static ServerMeta ToApplicationModel(this ServerEntity entity)
            => new ServerMeta(entity.Name,
                entity.UriAddress,
                entity.IpAddress,
                entity.Ping,
                entity.WebUrl,
                entity.Language,
                entity.Gamemode,
                entity.Version,
                entity.PlayersCount,
                entity.MaxPlaers,
                entity.Players,
                entity.IsLagcomp,
                entity.IsSampCac,
                entity.IsOpenMp,
                entity.HasPassword);
    }
}
