using SimpleLauncher.Domain.Models;
using System.Diagnostics;

namespace SimpleLauncher.Domain.Abstractions
{
    public interface IGameProcessManager
    {
        Task<(Process? gameProcess, string errorMessage)> StartAndConnectAsync(string gameExecutablePath, 
            string clientExecutablePath, 
            string additionalArguments, 
            string serverIpAndPort, 
            List<GameAddon> addons,
            CancellationToken cancellationToken);
    }
}