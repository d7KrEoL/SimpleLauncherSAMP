namespace SimpleLauncher.Infrastructure.MonitorAPI.Contracts.OpenMP
{
    public record GetServerInfoResponse(string ip,
        string? dm,
        GetServerInfoResponseCore core,
        GetServerInfoResponseRu ru,
        string? description,
        string? banner,
        bool active,
        bool pending,
        DateTime lastUpdated,
        DateTime lastActive);
    public record GetServerInfoResponseCore(string ip,
        string hn, // Hostname
        int pc, // Players count
        int pm, // Players max
        string gm, // Game mode
        string la, // Language
        bool pa, // ??Password??
        string vn, // Version number
        bool omp, // Is open mp
        bool pr); // ??Is password required??
    public record GetServerInfoResponseRu(string allowed_clients, // example: "0.3.7, 0.3.DL"
        string artwork, // example: "Yes"
        string lagcomp, // example: "On"
        string mapname, // example: "San Andreas"
        string version, // example: "omp 1.4.0.2783"
        string weather, // example: "10"
        string weburl, // example: "www.youtube.com/@theziomsmedia"
        string worldtime); // example: "12:00"
}
