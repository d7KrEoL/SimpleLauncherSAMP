using Microsoft.Extensions.Logging;
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
        internal static Process? StartProcessSuspended(ProcessStartInfo startInfo,
            ILogger? logger = default)
        {
            string commandLine = $"\"{startInfo.FileName}\" {startInfo.Arguments}";
            logger?.LogInformation("commandLine: {CommandLine}", commandLine);
            NativeImports.StartupInfo si = new NativeImports.StartupInfo();
            NativeImports.ProcessInformation pi = new NativeImports.ProcessInformation();

            bool success = NativeImports.CreateProcessA(null,
                commandLine,
                false,
                NativeImports.CreateSuspendedFlag | NativeImports.DetachedProcess,
                startInfo.WorkingDirectory,
                ref si,
                out pi);

            if (!success || pi.hProcess == IntPtr.Zero)
                return null;
            try
            {
                return Process.GetProcessById(pi.dwProcessId);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to get process by ID after creation.");
                return null;
            }
        }
        internal static void ResumeProcess(Process process)
        {
            if (process == null || process.HasExited || process.Threads.Count == 0)
                return;
            int mainThreadId = process.Threads[0].Id;

            IntPtr hThread = NativeImports.OpenThread(NativeImports.ThreadAllAccess,
                false,
                mainThreadId);
            if (hThread == IntPtr.Zero)
                return;
            try
            {
                NativeImports.ResumeThread(hThread);
            }
            finally
            {
                NativeImports.CloseHandle(hThread);
            }
        }
    }
}
