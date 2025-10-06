using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SimpleLauncher.Presentation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> LogEntries => _logService.LogEntries;
        private const int MaxLogMessages = 100;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogService _logService;
        private readonly ILogger<MainWindow> _logger;
        private readonly IServerListService _serverListService;
        private readonly ISampQueryAdapter _sampQuery;
        private ServerInfoWindow? _serverInfoWindow;
        private CancellationTokenSource? _currentOperationCancellationTokenSource;
        private ObservableCollection<ServerMeta> _serverList = new ObservableCollection<ServerMeta>();
        private CollectionViewSource _serverListViewSource;
        public MainWindow(IServiceProvider serviceProvider,
            ILogService logService,
            ILogger<MainWindow> logger,
            IServerListService serverList,
            ISampQueryAdapter sampQuery)
        {
            _serviceProvider = serviceProvider;
            _logService = logService;
            _logger = logger;
            _logger.LogInformation($"Program started");
            _serverListService = serverList;
            _sampQuery = sampQuery;

            InitializeComponent();
            _serverListViewSource = (CollectionViewSource)FindResource("ServerListViewSource");
            _serverListViewSource.Source = _serverList;
            ((INotifyCollectionChanged)_logBox.Items).CollectionChanged += _logBox_ItemsChanged;
            //_serverListView.ItemsSource = _serverList;

            DataContext = this;

            AddClient("1");
            AddClient("2");
            AddClient("3");
            AddClient("1");
            AddClient("2");

            _monitorListMenu.Items.Add("I");
            _monitorListMenu.Items.Add("II");
            _monitorListMenu.Items.Add("III");

            _versionLabel.Text = "ver. 01.10.2025";

            _ = LoadServersAsync();
            _logger.LogInformation("Program loaded!");
        }
        private async Task LoadServersAsync()
        {
            _logger.LogInformation("LoadServersAsync");
            try
            {
                _logger.LogInformation("Starting servers update...");
                await RecreateCurrentOperation();
                if (_currentOperationCancellationTokenSource is null)
                    return;
                var servers = await _serverListService.GetServers(_currentOperationCancellationTokenSource.Token);
                _logger.LogInformation("Data received: {COUNT} servers", servers?.Count());
                await Dispatcher.InvokeAsync(() =>
                {
                    if (servers is null || !servers.Any())
                    {
                        _logger.LogWarning("No servers received");
                        return;
                    }
                    _serverList.Clear();
                    foreach (var server in servers)
                    {
                        _serverList.Add(server);
                    }
                    _logger.LogInformation("UI updated with {COUNT} servers", servers?.Count());
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading servers");
            }
            _logger.LogInformation("LoadServersAsync done");
        }
        public void AddClient(string clientPath)
        {
            if (!_clientListMenu.Items
                .OfType<string>()
                .Any(item => item.Equals(clientPath)))
                _clientListMenu.Items.Add(clientPath);
            else
                _logger.LogDebug("Element {PATH} is already exist", clientPath);
        }

        private void _clearLog_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Clear log?", "Clear?", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question) == MessageBoxResult.Yes)
                    _logService.ClearLogs();
        }

        private void _refreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serverListService is null)
                return;
            _ = LoadServersAsync();
        }

        private void _logBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_logBox.SelectedIndex == -1)
                return;
            var text = _logBox?.SelectedItem?.ToString();
            if (text is null)
                return;
            Clipboard.SetText(text);
        }
        private async Task CancelCurrentOperation()
        {
            if (_currentOperationCancellationTokenSource is null)
                return;
            await _currentOperationCancellationTokenSource.CancelAsync();
            _currentOperationCancellationTokenSource.Dispose();
        }
        private async Task RecreateCurrentOperation()
        {
            if (_currentOperationCancellationTokenSource is null ||
                _currentOperationCancellationTokenSource.IsCancellationRequested)
            {
                _currentOperationCancellationTokenSource?.Dispose();
                _currentOperationCancellationTokenSource = new CancellationTokenSource();
                return;
            }
            await _currentOperationCancellationTokenSource.CancelAsync();
            _currentOperationCancellationTokenSource?.Dispose();
            _currentOperationCancellationTokenSource = new CancellationTokenSource();
        }

        private async void _serverListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var server = _serverListView.SelectedItem as ServerMeta;
            if (server is null)
            {
                _logger.LogError("Cannot open server info: cannot find selected server");
                return;
            }
            if (_serverInfoWindow is not null)
            {
                await CancelCurrentOperation();
                _serverInfoWindow.Close();
                _serverInfoWindow = null;
            }
            await RecreateCurrentOperation();
            _serverInfoWindow = _serviceProvider.GetRequiredService<ServerInfoWindow>();
            _serverInfoWindow.Show();
            List<PlayerMeta> players;
            if (server.PlayersCount <= 50)
            {
                try
                {
                    if (_currentOperationCancellationTokenSource is null)
                        return;

                    players = await _sampQuery.GetServerPlayersAsync(server.IpAddress,
                        _currentOperationCancellationTokenSource.Token);
                }
                catch (SocketException ex)
                {
                    _logger.LogError(ex, "Cannot connect to samp server query: {SERVER}", server.IpAddress);
                    players = new List<PlayerMeta>() { new PlayerMeta(0, "Cannot connect", 0, 0) };
                }
                catch (ArgumentException ex)
                {
                    _logger.LogError(ex, "Wrong samp server ip:port was taken {IPPORT}", server.IpAddress);
                    players = new List<PlayerMeta>() { new PlayerMeta(0, "Wrong server data", 0, 0) };
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogWarning(ex, "Operation was cancelled");
                    players = new List<PlayerMeta>() { new PlayerMeta(0, "Request was cancelled", 0, 0) };
                }
            }
            else
                players = new List<PlayerMeta>() { new PlayerMeta(0, "So many players...", 0, 0) };

            var serverInfo = server;
            var playersInfo = players;
            var cancellationToken = _currentOperationCancellationTokenSource?.Token;
            if (cancellationToken is null)
            {
                _serverInfoWindow.Close();
                return;
            }
            try
            {
                _ = _serverInfoWindow.Configure(serverInfo,
                playersInfo,
                cancellationToken ?? new CancellationToken(true));
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogDebug(ex, "Operation in serverInfo window was cancelled");
                _serverInfoWindow.Close();
            }
            _logger.LogInformation("Server selected:\n{NAME}\n{IP}", server.Name, server.IpAddress);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _serverInfoWindow?.Close();
            _currentOperationCancellationTokenSource?.CancelAsync();
        }

        private void _logBox_ItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var itemsCount = _logBox.Items.Count;
            if (itemsCount < 1)
                return;
            _logBox.ScrollIntoView(_logBox.Items[_logBox.Items.Count - 1]);
            if (itemsCount > MaxLogMessages)
            {
                for (int i = 0; i < 20; i++)
                    _logBox.Items.RemoveAt(i);
            }
        }

        private void _hideExtraMenuButton_Click(object sender, RoutedEventArgs e)
        {
            const int ExtraMenuGridHeight = 20;
            if (_nicknameInput.IsVisible)
            {
                _extraPanel.Height = GridLength.Auto;
                _nicknameInput.Visibility = Visibility.Collapsed;
                _nickNameTextBlock.Visibility = Visibility.Collapsed;
                _hideExtraMenuButton.Content = "↓";
            }
            else
            {
                _extraPanel.Height = new GridLength(ExtraMenuGridHeight);
                _nicknameInput.Visibility = Visibility.Visible;
                _nickNameTextBlock.Visibility = Visibility.Visible;
                _hideExtraMenuButton.Content = "↑";
            }
        }
        // Метод для фильтрации
        private void ApplyFilter(string propertyName, string filterValue)
        {
            if (_serverListViewSource?.View != null)
            {
                if (string.IsNullOrWhiteSpace(filterValue) || filterValue == "*")
                {
                    ClearFilter();
                    return;
                }
                _serverListViewSource.View.Filter = item =>
                {
                    if (item is ServerMeta server)
                    {
                        var property = typeof(ServerMeta).GetProperty(propertyName);
                        if (property is not null)
                        {
                            var value = property.GetValue(server)?.ToString();
                            return value?.Contains(filterValue, StringComparison.OrdinalIgnoreCase) == true;
                        }
                    }
                    return false;
                };
            }
        }
        private void ApplyNotFilter(string propertyName, string filterValue)
        {
            if (_serverListViewSource?.View != null)
            {
                if (string.IsNullOrWhiteSpace(filterValue) || filterValue == "*")
                {
                    ClearFilter();
                    return;
                }
                _serverListViewSource.View.Filter = item =>
                {
                    if (item is ServerMeta server)
                    {
                        var property = typeof(ServerMeta).GetProperty(propertyName);
                        if (property is not null)
                        {
                            var value = property.GetValue(server)?.ToString();
                            return value?.Contains(filterValue, StringComparison.OrdinalIgnoreCase) == false;
                        }
                    }
                    return false;
                };
            }
        }
        private void ApplyComparingFilter(string propertyName, string compareToPropertyName)
        {
            if (_serverListViewSource?.View != null)
            {
                if (string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(compareToPropertyName))
                {
                    ClearFilter();
                    return;
                }
                _serverListViewSource.View.Filter = item =>
                {
                    if (item is ServerMeta server)
                    {
                        var property = typeof(ServerMeta).GetProperty(propertyName);
                        var propertyComparingTo = typeof(ServerMeta).GetProperty(compareToPropertyName);
                        if (property is not null && propertyComparingTo is not null)
                        {
                            var value1 = property.GetValue(server);
                            var value2 = propertyComparingTo.GetValue(server);
                            return Equals(value1, value2);
                        }
                    }
                    return false;
                };
            }
        }

        // Сброс фильтра
        private void ClearFilter()
        {
            if (_serverListViewSource?.View != null)
            {
                _serverListViewSource.View.Filter = null;
            }
        }

        // Примеры фильтрации
        private void FilterByName(string name)
        {
            ApplyFilter("Name", name);
        }

        private void FilterByGamemode(string gamemode)
        {
            ApplyFilter("Gamemode", gamemode);
        }

        private void FilterByLanguage(string language)
        {
            ApplyFilter("Language", language);
        }
        private void FilterByLagcomp(bool check)
        {
            if (check)
                ApplyFilter("IsLagcomp", "True");
            else
                ApplyFilter("IsLagcomp", string.Empty);
        }
        private void FilterByLagshot(bool check)
        {
            if (check)
                ApplyFilter("IsLagcomp", "False");
            else
                ApplyFilter("IsLagcomp", string.Empty);
        }
        private void FilterByEmpty(bool check)
        {
            if (check)
                ApplyNotFilter("PlayersCount", "0");
            else
                ApplyNotFilter("PlayersCount", string.Empty);
        }
        private void FilterByFull(bool check)
        {
            if (check)
                ApplyComparingFilter("PlayersCount", "MaxPlaeyers");
            else
                ApplyComparingFilter(string.Empty, string.Empty);
        }
        private void FilterByPassword(bool check)
        {
            if (check)
                ApplyFilter("HasPassword", "False");
            else
                ApplyFilter("HasPassword", string.Empty);
        }
        private void FilterByOmp(bool check)
        {
            if (check)
                ApplyFilter("IsOpenMp", "True");
            else
                ApplyFilter("IsOpenMp", string.Empty);
        }
        private void FilterBySamp(bool check)
        {
            if (check)
                ApplyFilter("IsOpenMp", "False");
            else
                ApplyFilter("IsOpenMp", string.Empty);
        }
        private void FilterBySampCac(bool check)
        {
            if (check)
                ApplyFilter("IsSampCac", "True");
            else
                ApplyFilter("IsSampCac", string.Empty);
        }

        private void _hostnameFilterInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox is null)
                return;
            FilterByName(textBox.Text);
        }

        private void _gamemodeFilterInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox is null)
                return;
            FilterByGamemode(textBox.Text);
        }

        private void _languageFilterInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox is null)
                return;
            FilterByLanguage(textBox.Text);
        }

        private void _isOnlyLagcompCheck_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem is null)
                return;
            _isOnlyLagshotCheck.IsChecked = false;
            FilterByLagcomp(menuItem.IsChecked);

        }

        private void _isOnlyLagshotCheck_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem is null)
                return;
            _isOnlyLagcompCheck.IsChecked = false;
            FilterByLagshot(menuItem.IsChecked);
        }

        private void _isNotEmptyCheck_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem is null) 
                return;
            _isNotFullCheck.IsChecked = false;
            FilterByEmpty(menuItem.IsChecked);
        }

        private void _isNotFullCheck_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem is null)
                return;
            _isNotEmptyCheck.IsChecked = false;
            FilterByFull(_isNotFullCheck.IsChecked);
        }

        private void _isNoPasswordCheck_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem is null)
                return;
            FilterByPassword(menuItem.IsChecked);
        }

        private void _isOnlyOpenMpCheck_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem is null)
                return;
            _isOnlySampCheck.IsChecked = false;
            FilterByOmp(menuItem.IsChecked);
        }

        private void _isOnlySampCheck_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem is null)
                return;
            _isOnlyOpenMpCheck.IsChecked = false;
            FilterBySamp(menuItem.IsChecked);
        }

        private void _isSampCacCheck_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem is null)
                return;
            FilterBySampCac(menuItem.IsChecked);
        }
    }
}