using System.Runtime.InteropServices;
using System.Text;

namespace SimpleLauncher.Infrastructure.Game.Utils
{
    internal static unsafe partial class NativeImports
    {
        internal const uint CreateSuspendedFlag = 0x4; // 0x00000004
        internal const uint DetachedProcess = 0x8; // 0x00000008
        internal const uint ThreadAllAccess = 0x1F03FF;
        internal const string APILibrary = "kernel32.dll";
        [LibraryImport(APILibrary, SetLastError = true)]
        internal static partial int ResumeThread(nint hThread);

        [LibraryImport(APILibrary, SetLastError = true)]
        internal static partial IntPtr VirtualAllocEx(nint hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [LibraryImport(APILibrary, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool VirtualFreeEx(nint hProcess, IntPtr lpAddress, int dwSize, uint dwFreeType);

        [LibraryImport(APILibrary, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool WriteProcessMemory(nint hProcess, IntPtr lpBaseAddress, ref byte lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [LibraryImport(APILibrary, SetLastError = true)]
        internal static partial IntPtr CreateRemoteThread(nint hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [LibraryImport(APILibrary, SetLastError = true)]
        internal static partial uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [LibraryImport(APILibrary, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        [LibraryImport(APILibrary, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CloseHandle(IntPtr hObject);

        [LibraryImport(APILibrary, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool TerminateProcess(nint hProcess, uint uExitCode);

        [DllImport(APILibrary)]
        internal static extern IntPtr OpenProcess(uint dwDesiredAccess, 
            bool bInheritHandle, 
            int dwProcessId);

        [DllImport(APILibrary)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, 
            string procName);

        [DllImport(APILibrary)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        /*[DllImport(APILibrary)]
        internal static extern IntPtr CreateRemoteThread(IntPtr hProcess, 
            IntPtr lpThreadAttributes,
            uint dwStackSize, 
            IntPtr lpStartAddress, 
            IntPtr lpParameter, 
            uint dwCreationFlags, IntPtr 
            lpThreadId);*/

        /*[DllImport(APILibrary, SetLastError = true)]
        internal static extern IntPtr VirtualAllocEx(IntPtr hProcess, 
            IntPtr lpAddress, 
            uint dwSize, 
            uint flAllocationType, 
            uint flProtect);*/

        /*[DllImport(APILibrary, SetLastError = true)]
        internal static extern bool VirtualFreeEx(IntPtr hProcess, 
            IntPtr lpAddress, 
            uint dwSize, 
            uint dwFreeType);*/

        /*[DllImport(APILibrary, SetLastError = true)]
        internal static extern bool WriteProcessMemory(IntPtr hProcess, 
            IntPtr lpBaseAddress, 
            ref byte lpBuffer, 
            uint nSize, 
            out int lpNumberOfBytesWritten);*/

        /*[DllImport(APILibrary, SetLastError = true)]
        internal static extern uint WaitForSingleObject(IntPtr hHandle, 
            uint dwMilliseconds);*/
        /*[DllImport(APILibrary, SetLastError = true)]
        internal static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);*/
        [DllImport(APILibrary, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process(IntPtr process, [MarshalAs(UnmanagedType.Bool)] out bool wow64Process);
        [DllImport(APILibrary, SetLastError = true)]
        /*internal static extern bool CloseHandle(IntPtr hObject);
        [DllImport(APILibrary, SetLastError = true, CharSet = CharSet.Unicode)]*/
        internal static extern bool CreateProcess(
            string? lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref StartupInfo lpStartupInfo,
            out ProcessInformation lpProcessInformation);

        [DllImport("kernel32.dll")]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, 
            IntPtr lpBaseAddress,
            byte[] lpBuffer, 
            int dwSize, 
            out int lpNumberOfBytesRead);

        /*[DllImport(APILibrary, SetLastError = true)]
        internal static extern uint ResumeThread(IntPtr hThread);*/

        [DllImport(APILibrary, SetLastError = true)]
        internal static extern IntPtr OpenThread(uint dwDesiredAccess, 
            bool bInheritHandle, 
            int dwThreadId);

        /*[DllImport(APILibrary, SetLastError = true, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateProcessA(
            string? lpApplicationName,
            IntPtr lpCommandLine,  // используем IntPtr вместо byte*
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string? lpCurrentDirectory,
            ref StartupInfo lpStartupInfo,
            out ProcessInformation lpProcessInformation);*/

        [DllImport(APILibrary, SetLastError = true, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateProcessA(
        string? lpApplicationName,
        IntPtr lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref StartupInfo lpStartupInfo,
        out ProcessInformation lpProcessInformation);

        public static bool CreateProcessA(
        string? applicationName,
        string? commandLine,
        bool inheritHandles,
        uint creationFlags,
        string? currentDirectory,
        ref StartupInfo startupInfo,
        out ProcessInformation processInformation)
        {
            IntPtr commandLinePtr = IntPtr.Zero;
            try
            {
                if (commandLine != null)
                {
                    byte[] commandLineBytes = Encoding.ASCII.GetBytes(commandLine + "\0");
                    commandLinePtr = Marshal.AllocHGlobal(commandLineBytes.Length);
                    Marshal.Copy(commandLineBytes, 0, commandLinePtr, commandLineBytes.Length);
                }

                return CreateProcessA(
                    applicationName,
                    commandLinePtr,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    inheritHandles,
                    creationFlags,
                    IntPtr.Zero,
                    currentDirectory,
                    ref startupInfo,
                    out processInformation);
            }
            finally
            {
                if (commandLinePtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(commandLinePtr);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StartupInfo
        {
            public uint cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
    }
}
