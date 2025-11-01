using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;

namespace SimpleLauncher.Presentation
{
    public class ListBoxLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ObservableCollection<string> _logEntries;

        public ListBoxLogger(string categoryName, ObservableCollection<string> logEntries)
        {
            _categoryName = categoryName;
            _logEntries = logEntries;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Information;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] [{logLevel}] {_categoryName}: {message}";

            if (exception != null)
            {
                logEntry += $"\nException: {exception.Message}";
            }

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                _logEntries.Add(logEntry);

                if (_logEntries.Count > 1000)
                {
                    _logEntries.RemoveAt(0);
                }
            });
        }
    }
}
