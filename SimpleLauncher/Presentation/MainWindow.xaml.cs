using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace SimpleLauncher.Presentation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> LogEntries => _logService.LogEntries;
        public delegate void AddFavoriteServerEventHandler(object sender,
            string ipAddress,
            AddFavoriteOrHistoryOperationResult operationResult);
        public delegate void AddHistoryServerEventHandler(object sender,
            string ipAddress,
            AddFavoriteOrHistoryOperationResult operationResult);
        public enum AddFavoriteOrHistoryOperationResult
        {
            Success,
            FailedAlreadyExist,
            FailedCannotPing,
            FailedBadAddress,
            FailedNotSet,
            Failed,
            Cancelled,
            InProgress
        }
        private const int MaxLogMessages = 100;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogService _logService;
        private readonly ILogger<MainWindow> _logger;
        private readonly ISampQueryAdapter _sampQuery;
        private readonly IServerListService _serverListService;
        private readonly IConfiguration _configuration;
        private readonly IConfigurationService _configurationService;
        private ServerInfoWindow? _serverInfoWindow;
        private AddFavoriteServerDialog? _addFavoriteServerDialog;
        private CancellationTokenSource? _currentOperationCancellationTokenSource;
        private ObservableCollection<ServerMeta> _serverList = new ObservableCollection<ServerMeta>();
        private ObservableCollection<IMonitoringApiGateway> _monitoringServices = new ObservableCollection<IMonitoringApiGateway>();
        private ObservableCollection<GameClient> _clientList = new ObservableCollection<GameClient>();
        private Dictionary<string, IMonitoringApiGateway> _monitoringServiceMap = new();
        private CollectionViewSource _serverListViewSource;
        private string? _currentSortColumn;
        private ListSortDirection _currentSortDirection = ListSortDirection.Ascending;
        private ServerListMode _currentServerListMode = ServerListMode.All;

        public MainWindow(IServiceProvider serviceProvider,
            ILogService logService,
            ILogger<MainWindow> logger,
            IServerListService serverList,
            ISampQueryAdapter sampQuery,
            IConfiguration configuration,
            IConfigurationService configurationService)
        {
            _serviceProvider = serviceProvider;
            _logService = logService;
            _logger = logger;
            _logger.LogInformation($"Program started");
            _serverListService = serverList;
            _sampQuery = sampQuery;
            _configuration = configuration;
            _configurationService = configurationService;

            _ = LoadMonitoringsAsync();
            _ = LoadClientPathsAsync();

            InitializeComponent();
            _serverListViewSource = (CollectionViewSource)FindResource("ServerListViewSource");
            _serverListViewSource.Source = _serverList;
            _monitorListMenu.ItemsSource = _monitoringServices;
            _clientListMenu.ItemsSource = _clientList;
            ((INotifyCollectionChanged)_logBox.Items).CollectionChanged += _logBox_ItemsChanged;

            DataContext = this;

            _versionLabel.Text = "ver. 01.10.2025";

            _ = LoadServersAsync();
            _logger.LogInformation("Program loaded!");
        }
        public async void OnAddServerToFavorites(object sender, 
            string ipAddress,
            AddFavoriteOrHistoryOperationResult operationResult)
        {
            const string FavoritesSectionName = "ServerList:Favorites";
            if (await OnAddServerToTab(FavoritesSectionName, sender, ipAddress, operationResult) !=
                AddFavoriteOrHistoryOperationResult.Success)
                _logger.LogError("Server was not added to history");
            await SwitchToFavorites();
            await Dispatcher.InvokeAsync(() => {
                var selectServer = _serverList
                        .FirstOrDefault(item => item.IpAddress.Equals(ipAddress));
                if (selectServer is null)
                    return;
                _clientListMenu_SelectionChanged(sender, new SelectionChangedEventArgs(Selector.SelectionChangedEvent,
                        new List<object>(),
                        _serverListView.SelectedItems));
            });
        }
        public async void OnAddServerToHistory(object sender,
            string ipAddress,
            AddFavoriteOrHistoryOperationResult operationResult)
        {
            const string HistorySectionName = "ServerList:LastConnected";
            if (await OnAddServerToTab(HistorySectionName, sender, ipAddress, operationResult) !=
                AddFavoriteOrHistoryOperationResult.Success)
                _logger.LogError("Server was NOT added to history");
            await SwitchToHistory();
            await Dispatcher.InvokeAsync(() => {
                var selectServer = _serverList
                        .FirstOrDefault(item => item.IpAddress.Equals(ipAddress));
                if (selectServer is null)
                    return;
                _clientListMenu_SelectionChanged(sender, new SelectionChangedEventArgs(Selector.SelectionChangedEvent,
                        new List<object>(),
                        _serverListView.SelectedItems));
            });
        }
        public async Task<AddFavoriteOrHistoryOperationResult> OnAddServerToTab(string sectionName,
            object sender,
            string ipAddress,
            AddFavoriteOrHistoryOperationResult operationResult)
        {
            switch (operationResult)
            {
                case AddFavoriteOrHistoryOperationResult.Success:
                    if (!_configurationService.AddValueToArray(sectionName, ipAddress))
                    {
                        _logger.LogError("Cannot add new server {SERVERNAME} into section: {SECTIONNAME}",
                            ipAddress,
                            sectionName);
                        return AddFavoriteOrHistoryOperationResult.FailedAlreadyExist;
                    }
                    return AddFavoriteOrHistoryOperationResult.Success;
                case AddFavoriteOrHistoryOperationResult.Cancelled:
                    _logger.LogTrace("Adding to favorites operation was cancelled");
                    return AddFavoriteOrHistoryOperationResult.Cancelled;
                default:
                    return AddFavoriteOrHistoryOperationResult.Failed;
            }
        }
        private async Task LoadClientPathsAsync()
        {
            var clientListSection = _configuration.GetSection("ClientList");

            if (clientListSection is null)
            {
                _logger.LogWarning("ClientList is not configured in settings.json");
                return;
            }
            foreach (var clientSection in clientListSection.GetChildren())
            {
                const string ExecutableFileNameShouldBe = "gta_sa.exe";
                var clientName = clientSection.Key;
                var version = clientSection.GetValue<string>("Version") ?? "Unknown";
                var path = clientSection.GetValue<string>("Path") ?? string.Empty;

                if (string.IsNullOrWhiteSpace(path) ||
                    !File.Exists(path) ||
                    !Path.GetFileName(path)
                        .Equals(ExecutableFileNameShouldBe,
                        StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Invalid client path: {PATH}", path);
                    continue;
                }
                await Dispatcher.InvokeAsync(() =>
                {
                    _logger.LogInformation("Game client found: {NAME} - {VERSION} ({PATH})", 
                        clientName, 
                        version, 
                        path);
                    AddClient(clientName, version, path);
                });
            }
        }
        private async Task LoadMonitoringsAsync()
        {
            var monitoringServices = _serviceProvider.GetServices<IMonitoringApiGateway>();
            await Dispatcher.InvokeAsync(() =>
            {
                _monitoringServices.Clear();
                foreach (var service in monitoringServices)
                {
                    _monitoringServices.Add(service);
                    _monitoringServiceMap.Add(service.Name, service);
                }
                _monitorListMenu.SelectedIndex = monitoringServices.Count() - 1;
            });
        }
        private async Task SetMonitoringServiceAsync(IMonitoringApiGateway service)
        {
            await _serverListService.UpdateMonitoringGatewayAsync(service);
        }
        private async Task LoadServersAsync()
        {
            switch(_currentServerListMode)
            {
                case ServerListMode.All:
                    await LoadServersFromMonitoringAsync();
                    break;
                case ServerListMode.Favorites:
                    await LoadServersFromFavoritesAsync();
                    break;
                case ServerListMode.History:
                    await LoadServersFromHistoryAsync();
                    break;
            }
        }
        private async Task LoadServersFromFavoritesAsync()
        {
            await LoadFromTabAsync("ServerList:Favorites");
        }
        private async Task LoadServersFromHistoryAsync()
        {
            await LoadFromTabAsync("ServerList:LastConnected");
        }
        private async Task LoadFromTabAsync(string sectionName)
        {
            var section = _configuration.GetSection(sectionName);
            if (!section.Exists())
            {
                _logger.LogWarning("No {SECTIONNAME} section found in configuration file", sectionName);
                return;
            }

            await Dispatcher.InvokeAsync(() => _serverList.Clear());

            var serverIps = section.GetChildren()
                .Select(s => s.Value)
                .OfType<string>()
                .Where(ip => !string.IsNullOrWhiteSpace(ip))
                .ToList();

            foreach (var ip in serverIps)
            {
                _ = Task.Run(async () =>
                {
                    _logger.LogInformation("Server from {SECTIONNAME}: {IP}", sectionName, ip);
                    var serverInfo = await _serverListService.GetServerInfoAsync(ip, 
                        CancellationToken.None);
                    if (serverInfo is null)
                    {
                        _logger.LogWarning("Could not get server info for: {IP}", ip);
                        await Dispatcher
                                .InvokeAsync(() => 
                                _serverList
                                .Add(ServerMeta
                                .CreateUnknown(null, ip, ip)));
                        return;
                    }
                    _logger.LogInformation("Server {NAME} added to list", serverInfo.Name);
                    await Dispatcher.InvokeAsync(() => _serverList.Add(serverInfo));
                });
            }
        }
        private async Task LoadServersFromMonitoringAsync()
        {
            _logger.LogInformation("LoadServersAsync");
            try
            {
                _logger.LogInformation("Starting servers update...");
                await RecreateCurrentOperation();
                if (_currentOperationCancellationTokenSource is null)
                    return;
                var servers = await _serverListService.GetServersAsync(_currentOperationCancellationTokenSource.Token);
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
        public void AddClient(string name, string version, string clientPath)
        {
            if (!_clientList
                .Any(item => item
                    .Name
                    .Equals(clientPath)))
                _clientList.Add(new GameClient(name, version, clientPath));
            else
                _logger.LogDebug("Element {NAME} ({PATH}) is already exist", 
                    name, 
                    clientPath);
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
                cancellationToken ?? new CancellationToken(true),
                _nicknameInput.Text,
                OnAddServerToFavorites,
                OnAddServerToHistory);
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
        private void _logBox_ItemsChanged(object sender, NotifyCollectionChangedEventArgs? e)
        {
            var itemsCount = _logBox.Items.Count;
            if (itemsCount < 1)
                return;
            _logBox.ScrollIntoView(_logBox.Items[_logBox.Items.Count - 1]);
            if (itemsCount > MaxLogMessages)
            {
                _logBox.Dispatcher.Invoke(() =>
                {
                    for (int i = 0; i < 20; i++)
                        LogEntries.RemoveAt(i);
                });
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
                _addFavoritesButton.Visibility = Visibility.Collapsed;
                _hideExtraMenuButton.Content = "↓";
            }
            else
            {
                _extraPanel.Height = new GridLength(ExtraMenuGridHeight);
                _nicknameInput.Visibility = Visibility.Visible;
                _nickNameTextBlock.Visibility = Visibility.Visible;
                _addFavoritesButton.Visibility = Visibility.Visible;
                _hideExtraMenuButton.Content = "↑";
            }
        }
        private readonly Dictionary<string, object?> _activeFilters = new();

        private void OrderBy(string columnName)
        {
            if (_serverListViewSource?.View == null)
                return;

            if (_currentSortColumn == columnName)
                _currentSortDirection = _currentSortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            else
            {
                _currentSortColumn = columnName;
                _currentSortDirection = ListSortDirection.Ascending;
            }

            _serverListViewSource.View.SortDescriptions.Clear();
            _serverListViewSource.View.SortDescriptions.Add(
                new SortDescription(columnName, _currentSortDirection)
            );
            _serverListViewSource.View.Refresh();
        }
        private bool UpdateFilterItem(string filterName, 
            object item, 
            ServerMeta server, 
            bool isInverted)
        {
            if (!_activeFilters
                .TryGetValue(filterName, out var fieldObj) ||
                fieldObj is not string field ||
                string.IsNullOrWhiteSpace(field))
                    return true;
            var propertyInfo = typeof(ServerMeta).GetProperty(filterName);
            if (propertyInfo is null)
                return true;
            var serverField = propertyInfo.GetValue(server) as string;
            if (serverField is null)
                return true;
            if (!serverField.Contains(field, StringComparison.OrdinalIgnoreCase))
            {
                if (isInverted)
                    return true;
                else
                    return false;
            }
            if (isInverted)
                return false;
            else
                return true;
        }
        private void UpdateServerListFilter()
        {
            if (_serverListViewSource?.View == null)
                return;

            _serverListViewSource.View.Filter = item =>
            {
                if (item is not ServerMeta server)
                    return false;

                // Фильтр по имени
                if (!UpdateFilterItem("Name", 
                    item, 
                    server, 
                    _isInvertedHostname.IsChecked ?? false))
                        return false;

                // Фильтр по режиму
                if (!UpdateFilterItem("Gamemode", 
                    item, 
                    server, 
                    _isInvertedGamemode.IsChecked ?? false))
                        return false;

                // Фильтр по языку
                if (!UpdateFilterItem("Language", 
                    item, 
                    server, 
                    _isInvertedLanguage.IsChecked ?? false)) 
                        return false;

                // Фильтр по Lagcomp
                if (_activeFilters.ContainsKey("IsLagcomp"))
                {
                    return server.IsLagcomp;
                }

                // Фильтр по Lagshot
                if (_activeFilters.ContainsKey("IsLagshot"))
                {
                    return !server.IsLagcomp;
                }

                // Фильтр по пустым
                if (_activeFilters.TryGetValue("NotEmpty", out var notEmptyObj) && notEmptyObj is bool notEmpty && notEmpty)
                {
                    if (server.PlayersCount == 0)
                        return false;
                }

                // Фильтр по заполненным
                if (_activeFilters.TryGetValue("Full", out var fullObj) && fullObj is bool full && full)
                {
                    if (server.PlayersCount.Equals(server.MaxPlaeyers))
                        return false;
                }

                // Фильтр по паролю
                if (_activeFilters.TryGetValue("NoPassword", out var noPasswordObj) && noPasswordObj is bool noPassword && noPassword)
                {
                    if (server.HasPassword)
                        return false;
                }

                // Фильтр по OpenMp
                if (_activeFilters.TryGetValue("IsOpenMp", out var isOmpObj) && isOmpObj is bool isOmp)
                {
                    if (server.IsOpenMp != isOmp)
                        return false;
                }

                // Фильтр по SampCac
                if (_activeFilters.TryGetValue("IsSampCac", out var isSampCacObj) && isSampCacObj is bool isSampCac)
                {
                    if (server.IsSampCac != isSampCac)
                        return false;
                }

                return true;
            };
        }

private void SetFilter(string key, object? value)
{
    if (value == null ||
        (value is string s && string.IsNullOrWhiteSpace(s)) ||
        (value is bool b && !b))
    {
        _activeFilters.Remove(key);
    }
    else
    {
        _activeFilters[key] = value;
    }
    UpdateServerListFilter();
}
        
private void FilterByName(string name) => SetFilter("Name", name);
private void FilterByGamemode(string gamemode) => SetFilter("Gamemode", gamemode);
private void FilterByLanguage(string language) => SetFilter("Language", language);
private void FilterByLagcomp(bool check) => SetFilter("IsLagcomp", check ? true : null);
private void FilterByLagshot(bool check) => SetFilter("IsLagshot", check ? true : null);
private void FilterByEmpty(bool check) => SetFilter("NotEmpty", check ? true : null);
private void FilterByFull(bool check) => SetFilter("Full", check ? true : null);
private void FilterByPassword(bool check) => SetFilter("NoPassword", check ? true : null);
private void FilterByOmp(bool check) => SetFilter("IsOpenMp", check ? true : null);
private void FilterBySamp(bool check) => SetFilter("IsOpenMp", check ? false : null);
private void FilterBySampCac(bool check) => SetFilter("IsSampCac", check ? true : null);

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
    FilterByLagshot(false);
    FilterByLagcomp(menuItem.IsChecked);
}

private void _isOnlyLagshotCheck_Click(object sender, RoutedEventArgs e)
{
    var menuItem = sender as MenuItem;
    if (menuItem is null)
        return;
    _isOnlyLagcompCheck.IsChecked = false;
    FilterByLagcomp(false);
    FilterByLagshot(menuItem.IsChecked);
}

private void _isNotEmptyCheck_Click(object sender, RoutedEventArgs e)
{
    var menuItem = sender as MenuItem;
    if (menuItem is null)
        return;
    FilterByEmpty(menuItem.IsChecked);
}

private void _isNotFullCheck_Click(object sender, RoutedEventArgs e)
{
    var menuItem = sender as MenuItem;
    if (menuItem is null)
        return;
    FilterByFull(menuItem.IsChecked);
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

        private void TextBlock_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OrderBy("Name");
        }

        private void TextBlock_MouseUp_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OrderBy("IpAddress");
        }

        private void TextBlock_MouseUp_2(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OrderBy("PlayersCount");
        }

        private void TextBlock_MouseUp_3(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OrderBy("MaxPlaeyers");
        }

        private void TextBlock_MouseUp_4(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OrderBy("Language");
        }

        private void TextBlock_MouseUp_5(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OrderBy("Gamemode");
        }

        private void TextBlock_MouseUp_6(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OrderBy("IsLagcomp");
        }

        private void TextBlock_MouseUp_7(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OrderBy("Version");
        }

        private void TextBlock_MouseUp_8(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OrderBy("Ping");
        }

        private void _monitorListMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = _monitorListMenu.SelectedItem as IMonitoringApiGateway;
            if (selectedItem is not null &&
                _monitoringServiceMap.TryGetValue(selectedItem.Name, out var service))
            {
                _logger.LogInformation("Selected monitoring service: {NAME}", selectedItem.Name);
                _monitoringSelectionText.Text = service.Name;
                _ = SetMonitoringServiceAsync(service);
                _refreshButton_Click(this, new RoutedEventArgs());
            }
        }

        private void _clientListMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            const string keyPath = @"SOFTWARE\SAMP";
            const string valueName = "gta_sa_exe";
            var selectedItem = _clientListMenu.SelectedItem as GameClient;
            if (selectedItem is null || 
                string.IsNullOrWhiteSpace(selectedItem.Path))
            {
                _logger.LogWarning("Selection was changed to none");
                return;
            }
            _logger.LogInformation("Selected client: {PATH}", selectedItem.Path);
            _clientListSelectionText.Text = selectedItem.Name;
            try
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\" + keyPath, 
                    valueName, 
                    selectedItem.Path, 
                    RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "[!] Error while changing current game registry key");
            }
        }
        private enum ServerListMode { 
            All, 
            Favorites, 
            History 
        }

        private void _allButton_Click(object sender, RoutedEventArgs e)
        {
            _currentServerListMode = ServerListMode.All;
            _ = LoadServersAsync();
        }
        private async Task SwitchToFavorites()
        {
            _currentServerListMode = ServerListMode.Favorites;
            await LoadServersAsync();
        }
        private async Task SwitchToHistory()
        {
            _currentServerListMode = ServerListMode.History;
            await LoadServersAsync();
        }
        private async void _favoritesButton_Click(object sender, RoutedEventArgs e)
        {
            await SwitchToFavorites();
        }

        private async void _historyButton_Click(object sender, RoutedEventArgs e)
        {
            _currentServerListMode = ServerListMode.History;
            await LoadServersAsync();
            //_ = LoadServersAsync();
        }

        private async void _addFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            
            if (_addFavoriteServerDialog is not null)
            {
                _addFavoriteServerDialog.Close();
                _addFavoriteServerDialog = null;
            }
            _addFavoriteServerDialog = _serviceProvider.GetRequiredService<AddFavoriteServerDialog>();
            await _addFavoriteServerDialog.ShowAddServerDialog(OnAddServerToFavorites);
            
            
        }

        private void _isInvertedHostname_Checked(object sender, RoutedEventArgs e)
        {
            UpdateServerListFilter();
        }

        private void _isInvertedHostname_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateServerListFilter();
        }

        private void _isInvertedGamemode_Checked(object sender, RoutedEventArgs e)
        {
            UpdateServerListFilter();
        }

        private void _isInvertedGamemode_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateServerListFilter();
        }

        private void _isInvertedLanguage_Checked(object sender, RoutedEventArgs e)
        {
            UpdateServerListFilter();
        }

        private void _isInvertedLanguage_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateServerListFilter();
        }
    }
}