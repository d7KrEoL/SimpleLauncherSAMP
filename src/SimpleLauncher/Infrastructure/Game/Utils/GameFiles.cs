using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace SimpleLauncher.Infrastructure.Game.Utils
{
    public static class GameFiles
    {
        private const string GameExecutableFilePattern = @"gta.*sa\.exe$";
        private const string SampClientExecutableFilePattern = @"sa.*mp\.dll$";
        /// <summary>
        /// Retrieves the path to the game executable by searching for a matching file in the directory of the specified
        /// client path.
        /// </summary>
        /// <remarks>
        /// The method searches for a file matching the pattern 'gta*sa.exe' (case-insensitive)
        /// within the same directory as the provided client path. If no matching file is found, the method returns 
        /// <see langword="null"/>.
        /// </remarks>
        /// <param name="clientPath">
        /// The full file path to the client executable. Must refer to an existing file within 
        /// the game's installation directory.
        /// </param>
        /// <returns>
        /// The full path to the game executable file if found; otherwise, <see langword="null"/>.
        /// </returns>
        /// <exception cref="DirectoryNotFoundException">
        /// Thrown if the directory containing <paramref name="clientPath"/> cannot be determined.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown if wrong samp client path was given (samp directory is empty).
        /// </exception>
        public static string? GetGameExecutablePathFromClientPath(string clientPath)
        {
            var clientPathDirectory = Path.GetDirectoryName(clientPath);
            if (clientPathDirectory is null)
                throw new DirectoryNotFoundException("Cannot find game directory from client path");
            var files = Directory.GetFiles(clientPathDirectory);
            return FindFileByPattern(clientPathDirectory, GameExecutableFilePattern)
                .result;
        }
        /// <summary>
        /// Retrieves the path to the samp client library by searching for a matching file in the 
        /// directory of the specified
        /// client path.
        /// </summary>
        /// <remarks>
        /// The method searches for a file matching the pattern 'sa*mp.exe' (case-insensitive)
        /// within the same directory as the provided client path. If no matching file is found, the method returns 
        /// <see langword="null"/>.
        /// </remarks>
        /// <param name="clientPath">
        /// The full file path to the client library. Must refer to an existing file within 
        /// the game's installation directory.
        /// </param>
        /// <returns>
        /// The full path to the client dll file if found; otherwise, <see langword="null"/>.
        /// </returns>
        /// <exception cref="DirectoryNotFoundException">
        /// Thrown if the directory containing <paramref name="clientPath"/> cannot be determined.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown if wrong samp client path was given (samp directory is empty).
        /// </exception>
        public static string? GetClientLibraryPathFromExecutablePath(string clientPath)
        {
            var clientPathDirectory = Path.GetDirectoryName(clientPath);
            if (clientPathDirectory is null)
                throw new DirectoryNotFoundException("Cannot find game directory from client path");
            return FindFileByPattern(clientPathDirectory, 
                SampClientExecutableFilePattern)
                .result;
        }

        public static string? GetGameExecutableInDirectory(string directoryPath)
        {
            var gameExecutable = FindFileByPattern(directoryPath, GameExecutableFilePattern);
            if (!string.IsNullOrEmpty(gameExecutable.error))
                return null;
            return string.IsNullOrWhiteSpace(gameExecutable.result) ? null : gameExecutable.result;
        }
        public static string? GetClientLibraryInDirectory(string directoryPath)
        {
            var gameExecutable = FindFileByPattern(directoryPath, SampClientExecutableFilePattern);
            if (!string.IsNullOrEmpty(gameExecutable.error))
                return null;
            return string.IsNullOrWhiteSpace(gameExecutable.result) ? null : gameExecutable.result;
        }

        public static bool IsGameDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(FindFileByPattern(path, 
                GameExecutableFilePattern).result))
                return false;
            if (string.IsNullOrWhiteSpace(FindFileByPattern(path, 
                SampClientExecutableFilePattern).result))
                return false;
            return true;
        }
        public static (string error, string result) FindFileByPattern(string directoryPath, string pattern)
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(directoryPath);
            }
            catch (IOException ex)
            {
                return ("Error accessing given directory path {ex}", ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ("Access to given directory path is denied {ex}", ex.Message);
            }
            catch (ArgumentException ex)
            {
                return ("Given directory path is invalid {ex}", ex.Message);
            }
            if (files.Length < 1)
                throw new FileNotFoundException("samp directory have no files (wrong samp directory was given)");
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return (string.Empty, 
                files.FirstOrDefault(fileName => 
                regex.IsMatch(Path.GetFileName(fileName))) ?? 
                string.Empty);
        }
    }
}
