using SimpleLauncher.Domain.Models;
using SimpleLauncher.Infrastructure.SampQuery.Models;

namespace SimpleLauncher.Infrastructure.SampQuery.Mappers
{
    public static class SampQueryPlayerInfoMapper
    {
        public static PlayerMeta ToApplicationModel(this QueryServerPlayer queryServerPlayer)
            => new PlayerMeta(queryServerPlayer.PlayerId,
                queryServerPlayer.PlayerName,
                queryServerPlayer.PlayerScore,
                queryServerPlayer.PlayerPing);
    }
}
