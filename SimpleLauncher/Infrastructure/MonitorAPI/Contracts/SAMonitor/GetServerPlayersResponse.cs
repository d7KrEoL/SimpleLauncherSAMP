using SimpleLauncher.Infrastructure.MonitorAPI.Models;

namespace SimpleLauncher.Infrastructure.MonitorAPI.Contracts.SAMonitor
{
    public record GetServerPlayersResponse(List<SAMonitorPlayer> players);
}
