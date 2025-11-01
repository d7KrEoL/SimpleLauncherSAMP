using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SimpleLauncher.Services
{
    public class ProcessInjectionService : IProcessInjectionService
    {
        private readonly ILogger<ProcessInjectionService> _logger;
        private Process? _process;
        private bool _disposed = false;

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes,
            uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        public ProcessInjectionService(ILogger<ProcessInjectionService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> InjectLibraryAsync(Process process, 
            string libraryPath,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            cancellationToken.Register(() =>
            {
                _logger.LogInformation("Injection operation was cancelled.");
            });
            if (process.HasExited)
            {
                _logger.LogError("Cannot inject into a process that has already exited.");
                return false;
            }
            _process = process;
            const Int32 ProcessAllAccess = 0x1F0FFF; // PROCESS_ALL_ACCESS
            if (_disposed) throw new ObjectDisposedException(nameof(ProcessInjectionService));

            var processHandle = OpenProcess(ProcessAllAccess, false, _process.Id);
            if (processHandle == IntPtr.Zero) return false;

            try
            {
                var kernel32 = GetModuleHandle("kernel32.dll");
                var loadLibrary = GetProcAddress(kernel32, "LoadLibraryW");

                // Дальнейшая логика инжекта...
                return true;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                    CloseHandle(processHandle);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _process?.Dispose();
                _disposed = true;
            }
        }

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
