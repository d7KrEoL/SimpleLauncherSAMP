using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Infrastructure.Game.Utils;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleLauncher.Services
{
    public class ProcessInjectionService : IProcessInjectionService
    {
        private const int InjectionTimeoutMs = 15000;
        private const uint MemoryAllocationTypeCommit = 0x1000;
        private const uint MemoryAllocationTypeReserve = 0x2000;
        private const uint MemoryAllocationTypeRelease = 0x8000;
        private const uint MemoryAllocationType = MemoryAllocationTypeCommit | MemoryAllocationTypeReserve;
        private const uint MemoryProtection = 0x04;
        private readonly ILogger<ProcessInjectionService> _logger;
        private Process? _process;
        private bool _disposed = false;

        public ProcessInjectionService(ILogger<ProcessInjectionService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> InjectLibraryAsync(Process process, 
            string libraryPath,
            CancellationToken cancellationToken)
        {
            const Int32 ProcessAllAccess = 0x1F0FFF; // PROCESS_ALL_ACCESS
            if (!File.Exists(libraryPath))
            {
                _logger.LogError("Error injecting library. File does not exist: {FilePath}", libraryPath);
                return false;
            }
            if (!libraryPath.All(c => c < 128))
            {
                _logger.LogError("Error injecting library. Library path contains non-ASCII characters: " +
                    "{FilePath}", libraryPath);
                return false;
            }
            if (!CheckDllDependencies(libraryPath))
            {
                _logger.LogError("Dll dependency check failed for: {LibraryPath}", libraryPath);
                return false;
            }
            if (!CheckFileAccess(libraryPath))
            {
                _logger.LogError("Dll file access check failed for: {LibraryPath}", libraryPath);
                return false;
            }
            _logger.LogInformation("Injecting library: {LibraryPath}", libraryPath);
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
            if (_disposed) throw new ObjectDisposedException(nameof(ProcessInjectionService));

            var processHandle = NativeImports.OpenProcess(ProcessAllAccess, false, process.Id);
            if (processHandle == IntPtr.Zero) 
                return false;
            nint allocAddress = IntPtr.Zero;
            nint remoteThread = IntPtr.Zero;
            try
            {
                var kernel32 = NativeImports.GetModuleHandle(NativeImports.APILibrary);
                // 'A' only for ASCII, 'W' for Unicode and will work correct with cyrillic paths
                var loadLibrary = NativeImports.GetProcAddress(kernel32, "LoadLibraryW");   // LoadLibraryW or LoadLibraryA?

                int libraryByteCount = Encoding.Unicode.GetByteCount(libraryPath);
                Span<byte> libraryPathBytes = stackalloc byte[libraryByteCount + 2];
                Encoding.Unicode.GetBytes(libraryPath, libraryPathBytes);
                libraryPathBytes[libraryByteCount] = 0;
                libraryPathBytes[libraryByteCount + 1] = 0;
                string res = string.Empty;

                NativeImports.IsWow64Process(processHandle, out bool isWow64);
                if (isWow64)
                {
                    _logger.LogInformation("Game process is working under Wow64 mode.");
                }

                allocAddress = NativeImports.VirtualAllocEx(processHandle,
                    IntPtr.Zero,
                    (uint)libraryPathBytes.Length,
                    MemoryAllocationType,
                    MemoryProtection);

                if (allocAddress == IntPtr.Zero)
                {
                    _logger.LogError("Cannot allocate memory in target process.");
                    return false;
                }
                CheckProcessAccessRights(processHandle, allocAddress);

                nint bytesWritten = 0;
                if (!NativeImports.WriteProcessMemory(processHandle,
                    allocAddress,
                    ref MemoryMarshal.GetReference(libraryPathBytes),
                    (uint)libraryPathBytes.Length,
                    out bytesWritten))
                {
                    _logger.LogError("Cannot write process memory");
                    return false;
                }
                _logger.LogInformation("Bytes written to target process: {BytesWritten}", bytesWritten);

                remoteThread = NativeImports.CreateRemoteThread(processHandle, 
                    IntPtr.Zero, 
                    0, 
                    loadLibrary, 
                    allocAddress, 
                    0, 
                    IntPtr.Zero);
                if (remoteThread == IntPtr.Zero)
                {
                    _logger.LogError("Cannot create remote thread to inject dll");
                    return false;
                }

                if (NativeImports.WaitForSingleObject(remoteThread,
                    InjectionTimeoutMs) != 0)
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogError("Timeout or error waiting for DLL injection to complete. " +
                        "ErrorCode: {ErrorCode}", error);
                    return false;
                }
                if (!NativeImports.GetExitCodeThread(remoteThread, out uint exitCode))
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogError("GetExitCodeThread failed. ErrorCode: {ErrorCode}", error);
                    return false;
                }
                if (exitCode == 0)
                {
                    _logger.LogError("Remote thread exited with code 0. LoadLibrary probably failed in target process.");
                    return false;
                }
                _logger.LogInformation("DLL injected successfully. Base address: 0x{Address:X8}", exitCode);
                return true;
            }
            finally
            {
                if (remoteThread != IntPtr.Zero)
                    NativeImports.CloseHandle(remoteThread);
                if (allocAddress != IntPtr.Zero &&  
                    processHandle != IntPtr.Zero)
                NativeImports.VirtualFreeEx(processHandle,
                    allocAddress,
                    0,
                    0x8000);
                if (processHandle != IntPtr.Zero)
                    NativeImports.CloseHandle(processHandle);
            }
        }

        private void CheckProcessAccessRights(IntPtr processHandle, IntPtr allocAddress)
        {
            try
            {
                byte[] buffer = new byte[4];
                if (NativeImports.ReadProcessMemory(processHandle, allocAddress, buffer, 4, out _))
                {
                    _logger.LogInformation("Process memory read access: OK");
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogError("Process memory read access failed: {ErrorCode}", error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Access check failed: {Message}", ex.Message);
            }
        }
        private bool CheckDllDependencies(string dllPath)
        {
            try
            {
                string architecture = GetDllArchitecture(dllPath);
                _logger.LogInformation("DLL architecture: {Architecture}", architecture);

                AnalyzeDllCharacteristics(dllPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to analyze DLL: {Message}", ex.Message);
                return false;
            }
        }

        private string GetDllArchitecture(string dllPath)
        {
            try
            {
                using var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read);
                using var br = new BinaryReader(fs);

                if (br.ReadUInt16() != 0x5A4D) // "MZ"
                    return "Invalid DOS header";

                fs.Seek(0x3C, SeekOrigin.Begin);
                var peOffset = br.ReadInt32();
                fs.Seek(peOffset, SeekOrigin.Begin);

                if (br.ReadUInt32() != 0x00004550) // "PE\0\0"
                    return "Invalid PE signature";

                var machine = br.ReadUInt16();

                return machine switch
                {
                    0x014C => "x86 (32-bit)",
                    0x8664 => "x64 (64-bit)",
                    0xAA64 => "ARM64",
                    0x01C0 => "ARM",
                    0x0200 => "IA64 (Itanium)",
                    _ => $"Unknown (0x{machine:X4})"
                };
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private void AnalyzeDllCharacteristics(string dllPath)
        {
            try
            {
                using var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read);
                using var br = new BinaryReader(fs);

                if (br.ReadUInt16() != 0x5A4D) return;

                fs.Seek(0x3C, SeekOrigin.Begin);
                var peOffset = br.ReadInt32();
                fs.Seek(peOffset, SeekOrigin.Begin);

                if (br.ReadUInt32() != 0x00004550) return;

                var machine = br.ReadUInt16();
                var numberOfSections = br.ReadUInt16();
                var timeDateStamp = br.ReadUInt32();
                var pointerToSymbolTable = br.ReadUInt32();
                var numberOfSymbols = br.ReadUInt32();
                var sizeOfOptionalHeader = br.ReadUInt16();
                var characteristics = br.ReadUInt16();

                bool isDll = (characteristics & 0x2000) != 0; // IMAGE_FILE_DLL
                bool isSystem = (characteristics & 0x1000) != 0; // IMAGE_FILE_SYSTEM

                _logger.LogInformation("DLL characteristics: IsDLL={IsDll}, IsSystem={IsSystem}, " +
                    "Sections={SectionsCount}, BuildTime={BuildTime}",
                    isDll, isSystem, numberOfSections,
                    DateTime.UnixEpoch.AddSeconds(timeDateStamp));
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not read DLL characteristics: {Message}", ex.Message);
            }
        }
        private bool CheckFileAccess(string dllPath)
        {
            try
            {
                var fileInfo = new FileInfo(dllPath);
                _logger.LogInformation("DLL file info: Size={Size} bytes, " +
                    "Exists={Exists}, ReadOnly={ReadOnly}",
                    fileInfo.Length, fileInfo.Exists, fileInfo.IsReadOnly);

                using var fs = File.OpenRead(dllPath);
                if (fs.ReadByte() == -1)
                {
                    _logger.LogError("Dll file is empty");
                    return false;
                }
                _logger.LogInformation("DLL file read access: OK");
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Wrong input dll file ArgumentException");
            }
            catch (PathTooLongException ex)
            {
                _logger.LogError(ex, "Injecting dll path is too long");
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, "Injecting dll directory not found");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Cannot inject! Access to dll file is restricted");
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Injecting dll file not found");
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError(ex, "Injecting dll file is not supported");
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Injecting dll file check IO exception");
            }
            catch (Exception ex)
            {
                _logger.LogError("DLL file access check failed (unexpected error): " +
                    "{Message}", ex.Message);
            }
            return false;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _process?.Dispose();
                _disposed = true;
            }
        }
    }
}
