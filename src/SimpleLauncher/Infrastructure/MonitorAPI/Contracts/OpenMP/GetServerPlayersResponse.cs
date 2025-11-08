namespace SimpleLauncher.Infrastructure.MonitorAPI.Contracts.OpenMP
{
    public record GetServerPlayersResponse(string ip,
        string? dm,
        GetServerInfoResponseCore core,
        GetServerInfoResponseRu ru,
        string? description,
        string? banner,
        bool active,
        bool pending,
        DateTime lastUpdated,
        DateTime lastActive);
    public record GetServerPlayersResponseCore(string ip,
        string hn,
        int pc,
        int pm,
        string gm,
        string la,
        bool pa,
        string vn,
        bool omp,
        bool pr);
    public record GetSErverPlayersResponseRules(string allowed_clients,
        string artwork,
        string lagcomp,
        string mapname,
        string version,
        string weather,
        string weburl,
        string worldtime);
}
