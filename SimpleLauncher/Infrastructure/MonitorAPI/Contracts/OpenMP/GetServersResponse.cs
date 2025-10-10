namespace SimpleLauncher.Infrastructure.MonitorAPI.Contracts.OpenMP
{
    public record GetServersResponse(string ip, 
        string hn,  // hostname
        uint pc, //  players count
        uint pm, //  players max
        string gm,  //  gamemode
        string la,  //  language
        bool pa,    //  ???
        string vn,  //  version number
        bool omp,   //  is openmp server
        bool pr);   //  ???password required
}
