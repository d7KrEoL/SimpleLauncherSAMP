using Microsoft.Extensions.Logging;
using SimpleLauncher.Presentation;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace SimpleLauncher.Logging
{
    public class ListBoxLoggerProvider : ILoggerProvider
    {
        private readonly ObservableCollection<string> _logEntries;
        private readonly ConcurrentDictionary<string, ListBoxLogger> _loggers = new();

        public ListBoxLoggerProvider(ObservableCollection<string> logEntries)
        {
            _logEntries = logEntries;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new ListBoxLogger(name, _logEntries));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
