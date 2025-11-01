namespace SimpleLauncher.Infrastructure.MonitorAPI.Contracts.SAMonitor
{
    public record GetServerByIpResponse(int id,
        bool success,
        DateTime lastUpdated,
        DateTime worldTime,
        uint playersOnline,
        uint maxPlayers,
        bool isOpenMp,
        bool lagComp,
        string name,
        string gameMode,
        string ipAddr,
        string mapName,
        string website,
        string version,
        string language,
        string sampCac,
        bool requiresPassword,
        int shuffledOrder,
        int weather,
        bool sponsor);
}
