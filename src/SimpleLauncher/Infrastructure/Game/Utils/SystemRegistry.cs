using Microsoft.Win32;
using System.IO;

namespace SimpleLauncher.Infrastructure.Game.Utils
{
    internal static class SystemRegistry
    {
        public static string FindGamePathInRegistry()
        {
            const string RegistryPath = @"SOFTWARE\SAMP";
            const string ValueName = "gta_sa_exe";
            const string SampExecutableName = "samp.exe";

            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
            if (key is null)
                throw new KeyNotFoundException("Cannot find installed samp instance");
            var gtaPath = key.GetValue(ValueName) as string;
            if (gtaPath is null)
                throw new FileNotFoundException("Cannot find gta_sa_exe registry key");
            /*var gameDirectory = Path.GetDirectoryName(gtaPath);
            if (string.IsNullOrEmpty(gameDirectory))
                throw new DirectoryNotFoundException("Cannot find gta game directory");
            var clientPath = Path.Combine(gameDirectory, SampExecutableName);
            if (!File.Exists(gameDirectory))
            {
                string[] exeFiles = Directory.GetFiles(gameDirectory, "*.exe");

                key = exeFiles?
                    .Where(file => file
                        .StartsWith("samp", StringComparison.OrdinalIgnoreCase))
                    .OfType<string>()?
                    .Take(1)?
                    .ToArray()[0] ??
                    throw new FileNotFoundException($"Cannot find {SampExecutableName} in samp directory");
            }*/
            return gtaPath;
        }
    }
}
