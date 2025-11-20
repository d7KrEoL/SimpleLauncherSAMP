namespace SimpleLauncher.Domain.Abstractions
{
    public interface IGameProcessManager
    {
        public enum GameLaunchInjectionType
        {
            SAMP,
            OMP
        }
        Task<string> StartAndConnectAsync(GameLaunchInjectionType injectType,
            string gamePath,
            string ip,
            string port,
            string nick,
            string password,
            CancellationToken cancellationToken);
    }
}