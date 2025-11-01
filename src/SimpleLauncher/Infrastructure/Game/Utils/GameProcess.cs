using System.Diagnostics;

namespace SimpleLauncher.Infrastructure.Game.Utils
{
    internal static class GameProcess
    {
        internal static async Task TerminateProcessAsync(Process process)
        {
            if (process.HasExited)
                return;
            process.CloseMainWindow();
            if (!process.WaitForExit(TimeSpan.FromSeconds(5)))
                process.Kill();
        }
    }
}
