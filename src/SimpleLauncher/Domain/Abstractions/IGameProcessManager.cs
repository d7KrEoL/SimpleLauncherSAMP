using SimpleLauncher.Domain.Models;
using System.Diagnostics;

namespace SimpleLauncher.Domain.Abstractions
{
    public interface IGameProcessManager
    {
        public enum GameLaunchInjectionType
        {
            SAMP,
            OMP
        }
        Task<(Process? gameProcess, string errorMessage)> StartAndConnectAsync(string gameExecutablePath, 
            string clientExecutablePath, 
            string additionalArguments, 
            string serverIpAndPort, 
            List<GameAddon> addons,
            CancellationToken cancellationToken);
        Task<string> StartAndConnectAsync(GameLaunchInjectionType injectType,
            string gamePath,
            string ip,
            string port,
            string nick,
            string password,
            CancellationToken cancellationToken);
    }
}