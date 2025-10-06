using SimpleLauncher.Domain.Abstractions;
using System.Collections.ObjectModel;
using System.Windows;

namespace SimpleLauncher.Services
{

    public class LogService : ILogService
    {
        public ObservableCollection<string> LogEntries { get; } = new();

        public void ClearLogs()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                LogEntries.Clear();
            });
        }
    }
}
