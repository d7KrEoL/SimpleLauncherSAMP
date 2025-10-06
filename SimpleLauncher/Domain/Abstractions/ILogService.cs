using System.Collections.ObjectModel;

namespace SimpleLauncher.Domain.Abstractions
{
    public interface ILogService
    {
        ObservableCollection<string> LogEntries { get; }
        /*void LogDebug(string message);
        void LogError(string message);
        void LogWarning(string message);
        void LogInformation(string message);
        void LogVerbose(string message);*/
        void ClearLogs();
    }
}
