using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SimpleLauncher.Application.Services;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace SimpleLauncher.Presentation.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ServerMeta> ServerList { get; } = new();
        public ObservableCollection<string> LogEntries { get; } = new();
        public ObservableCollection<IMonitoringApiGateway> MonitoringServices { get; } = new();
        public ObservableCollection<GameClient> ClientList { get; }

        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigurationService _configurationService;
        private readonly IGameClientService _gameClientService;
        private readonly Dictionary<string, object?> _activeFilters = new();
        private static readonly Regex NicknameValidCharsRegex = new Regex(@"^[A-Za-z0-9_\.\[\]@!#$]*$");
        private AddFavoriteServerDialog? _addFavoriteServerDialog;
        private AddGameClientWindow? _addGameClientWindow; 
        private ServerInfoWindow? _serverInfoWindow;
        private string _nickname = "Nickname";
        public string Nickname
        {
            get => _nickname;
            set {
                if (!IsValidNicknameField(value))
                    return;
                _nickname = value; 
                OnPropertyChanged(); 
            }
        }

        private IMonitoringApiGateway? _selectedMonitoringService;
        public IMonitoringApiGateway? SelectedMonitoringService
        {
            get => _selectedMonitoringService;
            set { _selectedMonitoringService = value; OnPropertyChanged(); OnMonitoringServiceChangedAsync(); }
        }

        private GameClient? _selectedClient;
        public GameClient? SelectedClient
        {
            get => _selectedClient;
            set { _selectedClient = value; OnPropertyChanged(); OnClientChanged(); }
        }

        private string _hostnameFilter = "";
        public string HostnameFilter
        {
            get => _hostnameFilter;
            set {
                if (value is not null)
                    _hostnameFilter = value;
                else
                    return;
                FilterByName(_hostnameFilter);
                OnPropertyChanged(); 
                UpdateServerListFilter(); 
            }
        }
        private string _gamemodeFilter = "";
        public string GamemodeFilter
        {
            get => _gamemodeFilter;
            set 
            { 
                if (value is not null)
                    _gamemodeFilter = value;
                else
                    return;
                FilterByGamemode(_gamemodeFilter);
                OnPropertyChanged(); 
                UpdateServerListFilter(); 
            }
        }
        private string _languageFilter = "";
        public string LanguageFilter
        {
            get => _languageFilter;
            set 
            { 
                if (value is not null)
                    _languageFilter = value;
                else
                    return;
                FilterByLanguage(_languageFilter);
                OnPropertyChanged(); 
                UpdateServerListFilter(); 
            }
        }
        private Dictionary<string, IMonitoringApiGateway> _monitoringServiceMap = new();
        public Dictionary<string, IMonitoringApiGateway> MonitoringServiceMap
        {
            get => _monitoringServiceMap;
            set { _monitoringServiceMap = value; OnPropertyChanged(); }
        }
        private ICollectionView? _serverListView;
        public ICollectionView ServerListView => 
            _serverListView ??= CollectionViewSource.GetDefaultView(ServerList);
        public bool IsInvertedHostname 
        {
            get; 
            set
            {
                field = value;
                OnPropertyChanged();
                UpdateServerListFilter();
            }
        }
        public bool IsInvertedGamemode 
        { 
            get;
            set
            {
                field = value;
                OnPropertyChanged();
                UpdateServerListFilter();
            }
        }
        public bool IsInvertedLanguage 
        { 
            get; 
            set
            {
                field = value;
                OnPropertyChanged();
                UpdateServerListFilter();
            }
        }

        // Дополнительные фильтры
        public bool IsOnlyLagcomp { 
            get;
            set { 
                field = value;
                if (value && IsOnlyLagshot)
                    IsOnlyLagshot = false;
                OnPropertyChanged();
                ToggleLagcompFilter();
            }
        }
        public bool IsOnlyLagshot 
        { 
            get; 
            set { 
                field = value;
                if (value && IsOnlyLagcomp)
                    IsOnlyLagcomp = false;
                OnPropertyChanged();
                ToggleLagshotFilter();
            }
        }
        public bool IsNotEmpty { get; set; }
        public bool IsNotFull { get; set; }
        public bool IsNoPassword { get; set; }
        public bool IsOnlyOpenMp 
        { 
            get;
            set 
            {
                field = value;
                if (value && IsOnlySamp)
                    IsOnlySamp = false;
                OnPropertyChanged();
                ToggleOnlyOpenMpFilter(); 
            } 
        }
        public bool IsOnlySamp 
        { 
            get; 
            set
            {
                field = value;
                if (value && IsOnlyOpenMp)
                    IsOnlyOpenMp = false;
                OnPropertyChanged();
                ToggleOnlySampFilter();
            }
        }
        public bool IsSampCac 
        { 
            get; 
            set
            {
                field = value;
                IsOnlySamp = value;
            }
        }

        private string? _currentSortColumn;
        private ListSortDirection _currentSortDirection = ListSortDirection.Ascending;
        private ServerListMode _currentServerListMode = ServerListMode.All;

        public ICommand RefreshCommand { get; }
        public ICommand AddFavoriteCommand { get; }
        public ICommand AddGameBuildCommand { get; }
        public ICommand AddHistoryCommand { get; }
        public ICommand SwitchFavoritesCommand { get; }
        public ICommand SwitchHistoryCommand { get; }
        public ICommand SwitchAllCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand SortCommand { get; }
        public ICommand ServerDoubleClickCommand { get; }
        public ICommand HideExtraMenuCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand LagcompFilterCommand { get; }
        public ICommand LagshotFilterCommand { get; }
        public ICommand NotEmptyFilterCommand { get; }
        public ICommand NotFullFilterCommand { get; }
        public ICommand NoPasswordFilterCommand { get; }
        public ICommand OnlyOpenMpFilterCommand { get; }
        public ICommand OnlySampFilterCommand { get; }
        public ICommand SampCacFilterCommand { get; }
        public ICommand CloseMainWindowCommand { get; }

        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IServerListService _serverListService;
        private readonly IConfiguration _configuration;
        private CancellationTokenSource? _currentOperationCancellationTokenSource;

        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            ILogService logService,
            IServerListService serverListService,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            IConfigurationService configurationService,
            IGameClientService gameClientService,
            ServerInfoWindow serverInfoWindow)
        {
            _logger = logger;
            _serverListService = serverListService;
            _configuration = configuration;
            LogEntries = logService.LogEntries;

            _serviceProvider = serviceProvider;
            _configurationService = configurationService;
            _serverInfoWindow = serverInfoWindow;
            _gameClientService = gameClientService;
            ClientList = (_gameClientService as GameClientService)?.Clients
                ?? new ObservableCollection<GameClient>();

            RefreshCommand = new RelayCommand(_ => _ = RefreshServers());
            SwitchFavoritesCommand = new RelayCommand(_ => SwitchFavorites());
            AddFavoriteCommand = new RelayCommand(_ => AddFavoriteServer());
            AddGameBuildCommand = new RelayCommand(_ => AddGameBuild());
            AddHistoryCommand = new RelayCommand(_ => AddFavoriteServer());
            SwitchHistoryCommand = new RelayCommand(_ => SwitchHistory());
            SwitchAllCommand = new RelayCommand(_ => SwitchAll());
            ClearLogCommand = new RelayCommand(_ => ClearLog());
            SortCommand = new RelayCommand(param => SortBy(param?.ToString()));
            ServerDoubleClickCommand = new RelayCommand(param => OnServerDoubleClickAsync(param));
            HideExtraMenuCommand = new RelayCommand(_ => HideExtraMenu());
            FilterCommand = new RelayCommand(_ => UpdateServerListFilter());
            LagcompFilterCommand = new RelayCommand(_ => ToggleLagcompFilter());
            LagshotFilterCommand = new RelayCommand(_ => ToggleLagshotFilter());
            NotEmptyFilterCommand = new RelayCommand(_ => ToggleNotEmptyFilter());
            NotFullFilterCommand = new RelayCommand(_ => ToggleNotFullFilter());
            NoPasswordFilterCommand = new RelayCommand(_ => ToggleNoPasswordFilter());
            OnlyOpenMpFilterCommand = new RelayCommand(_ => ToggleOnlyOpenMpFilter());
            OnlySampFilterCommand = new RelayCommand(_ => ToggleOnlySampFilter());
            SampCacFilterCommand = new RelayCommand(_ => ToggleSampCacFilter());
            CloseMainWindowCommand = new RelayCommand(_ => CloseMainWindow());
        }
        public async Task InitializeAsync()
        {
            await LoadMonitoringsAsync();
            await LoadClientPathsAsync();
            await LoadServersByModeAsync(_currentServerListMode);
        }
        public async void OnAddServerToFavorites(object sender,
            string ipAddress,
            AddFavoriteOrHistoryOperationResult operationResult)
        {
            if (operationResult.Equals(AddFavoriteOrHistoryOperationResult.Cancelled))
            {
                _logger.LogTrace("Adding to favorites operation was cancelled by user");
                return;
            }
            const string FavoritesSectionName = "ServerList:Favorites";
            if (await OnAddServerToTab(FavoritesSectionName, sender, ipAddress, operationResult) !=
                AddFavoriteOrHistoryOperationResult.Success)
                _logger.LogError("Server was not added to history");
            SwitchFavorites();
            var selectServer = ServerList
                    .FirstOrDefault(item => item.IpAddress.Equals(ipAddress));
            if (selectServer is null)
                return;
        }
        public async void OnAddServerToHistory(object sender,
            string ipAddress,
            AddFavoriteOrHistoryOperationResult operationResult)
        {
            const string HistorySectionName = "ServerList:LastConnected";
            if (await OnAddServerToTab(HistorySectionName, sender, ipAddress, operationResult) !=
                AddFavoriteOrHistoryOperationResult.Success)
                _logger.LogError("Server was NOT added to history");
            SwitchHistory();
            var selectServer = ServerList
                    .FirstOrDefault(item => item.IpAddress.Equals(ipAddress));
            if (selectServer is null)
                return;
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
        public void MonitoringChanged()
        {

        }

        private async Task RecreateCurrentOperation()
        {
            try
            {
                if (_currentOperationCancellationTokenSource?.Token is not null)
                    await _currentOperationCancellationTokenSource.CancelAsync();
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogDebug(ex, "CancellationTokenSource was already disposed");
            }
            _currentOperationCancellationTokenSource = new CancellationTokenSource();
        }
        private async Task CancelCurrentOperation()
        {
            if (_currentOperationCancellationTokenSource is null)
                return;
            await _currentOperationCancellationTokenSource.CancelAsync();
            _currentOperationCancellationTokenSource.Dispose();
        }
        private async Task RefreshServers() 
        { 
            await LoadServersByModeAsync(_currentServerListMode); 
        }
        private async Task LoadServersByModeAsync(ServerListMode mode)
        {
            ServerList.Clear();
            ServerList.Add(new ServerMeta("Loading...",
                string.Empty,
                string.Empty,
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                0,
                new List<string>(),
                false,
                false,
                false,
                false));
            try
            {
                switch (mode)
                {
                    case ServerListMode.All:
                        await LoadServersFromMonitoringAsync();
                        break;
                    case ServerListMode.Favorites:
                        await LoadFromTabAsync("ServerList:Favorites");
                        break;
                    case ServerListMode.History:
                        await LoadFromTabAsync("ServerList:LastConnected");
                        break;
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogTrace(ex, "Operation was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while loading server list");
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
                ServerList.Clear();
                if (servers is null || !servers.Any())
                {
                    _logger.LogWarning("No servers received");
                    return;
                }
                foreach (var server in servers)
                {
                    ServerList.Add(server);
                }
                _logger.LogInformation("UI updated with {COUNT} servers", servers?.Count());
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Operation was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading servers");
            }
            _logger.LogInformation("LoadServersAsync done");
        }
        private async Task LoadFromTabAsync(string sectionName)
        {
            await RecreateCurrentOperation();
            var section = _configuration.GetSection(sectionName);
            if (!section.Exists())
            {
                _logger.LogWarning("No {SECTIONNAME} section found in configuration file", sectionName);
                return;
            }

            ServerList.Clear();

            var serverIps = section.GetChildren()
                .Select(s => s.Value)
                .OfType<string>()
                .Where(ip => !string.IsNullOrWhiteSpace(ip))
                .ToList();

            await LoadServersInfoAsync(serverIps, sectionName);
        }
        private async Task LoadServersInfoAsync(List<string> serverIps, 
            string sectionName)
        {
            var tasks = serverIps.Select(async ip =>
            {
                try
                {
                    if (_currentOperationCancellationTokenSource?.Token is null)
                    {
                        _logger.LogError("Unexpected cancellation token value (null)");
                        return;
                    }
                    _currentOperationCancellationTokenSource.Token.ThrowIfCancellationRequested();
                    _logger.LogInformation("Server from {SECTIONNAME}: {IP}", sectionName, ip);
                    var serverInfo = await _serverListService
                        .GetServerInfoAsync(ip,
                            _currentOperationCancellationTokenSource.Token);
                    _currentOperationCancellationTokenSource.Token.ThrowIfCancellationRequested();
                    if (serverInfo is null)
                    {
                        _logger.LogWarning("Could not get server info for: {IP}", ip);
                        ServerList.Add(ServerMeta.CreateUnknown(null, ip, ip));
                    }
                    _logger.LogInformation("Server {NAME} added to list", serverInfo.Name);
                    ServerList.Add(serverInfo);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogInformation(ex, "Get server info operation was cancelled");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading server info for: {IP}", ip);
                    ServerList.Add(ServerMeta.CreateUnknown(null, ip, ip));
                }
            });
            await Task.WhenAll(tasks);
        }
        private async Task LoadMonitoringsAsync()
        {
            var monitoringServices = _serviceProvider.GetServices<IMonitoringApiGateway>();
            MonitoringServices.Clear();
            foreach (var service in monitoringServices)
            {
                MonitoringServices.Add(service);
                MonitoringServiceMap.Add(service.Name, service);
            }
            SelectedMonitoringService = MonitoringServices.FirstOrDefault();
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
                var clientName = clientSection.Key;
                var version = clientSection.GetValue<string>("Version") ?? "Unknown";
                var path = clientSection.GetValue<string>("Path") ?? string.Empty;

                if (string.IsNullOrWhiteSpace(path))
                {
                    _logger.LogWarning("Invalid client path: {PATH}", path);
                    continue;
                }
                _logger.LogInformation("Game client found: {NAME} - {VERSION} ({PATH})",
                    clientName,
                    version,
                    path);
                AddClient(clientName, version, path);
            }
            SelectedClient = ClientList.FirstOrDefault();
        }
        public void AddClient(string name, string version, string clientPath)
        {
            _logger.LogInformation("Adding client {NAME} {VERSION} {PATH}...",
                name,
                version,
                clientPath);
            var gameClient = _gameClientService
                .AddGameClientAsync(name, version, clientPath);
            if (gameClient is null)
            {
                _logger.LogInformation("Cannot add game client {NAME} {VERSION} {PATH}",
                    name,
                    version,
                    clientPath);
            }
        }
        private async void AddFavoriteServer() 
        {
            if (_addFavoriteServerDialog is not null)
            {
                _addFavoriteServerDialog.Close();
                _addFavoriteServerDialog = null;
            }
            _addFavoriteServerDialog = _serviceProvider.GetRequiredService<AddFavoriteServerDialog>();
            await _addFavoriteServerDialog.ShowAddServerDialog(OnAddServerToFavorites);
        }
        private async void AddGameBuild()
        {
            if (_addGameClientWindow is not null)
            {
                _addGameClientWindow.Close();
                _addGameClientWindow = null;
            }
            _addGameClientWindow = _serviceProvider.GetRequiredService<AddGameClientWindow>();
            _addGameClientWindow.Show();
            _logger.LogWarning("AddGameBuild in ViewModel is empty");
        }
        private async void _favoritesButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchFavorites();
        }
        private void AddHistoryServer() 
        {
            throw new NotImplementedException();
        }
        private async void SwitchFavorites() { _currentServerListMode = ServerListMode.Favorites; await RefreshServers(); }
        private async void SwitchHistory() { _currentServerListMode = ServerListMode.History; await RefreshServers(); }
        private async void SwitchAll() { _currentServerListMode = ServerListMode.All; await RefreshServers(); }
        private void ClearLog() { LogEntries.Clear(); }
        private void SortBy(string? columnName) 
        {
            if (ServerListView == null)
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

            ServerListView.SortDescriptions.Clear();
            ServerListView.SortDescriptions.Add(
                new SortDescription(columnName, _currentSortDirection)
            );
            ServerListView.Refresh();
        }
        private async void OnServerDoubleClickAsync(object? param) 
        {
            var server = param as ServerMeta;
            if (server is null)
            {
                _logger.LogError("Cannot open server info: cannot find selected server");
                return;
            }
            _logger.LogInformation("Server selected:\n{NAME}\n{IP}", server.Name, server.IpAddress);
        }
        private void HideExtraMenu() { }
        private void ToggleLagcompFilter() 
        {
            FilterByLagcomp(IsOnlyLagcomp);
        }
        private void ToggleLagshotFilter() 
        { 
            FilterByLagshot(IsOnlyLagshot);
        }
        private void ToggleNotEmptyFilter() 
        { 
            FilterByEmpty(IsNotEmpty);
        }
        private void ToggleNotFullFilter() 
        { 
            FilterByFull(IsNotFull);
        }
        private void ToggleNoPasswordFilter() 
        { 
            FilterByPassword(IsNoPassword); 
        }
        private void ToggleOnlyOpenMpFilter() 
        { 
            FilterByOmp(IsOnlyOpenMp); 
        }
        private void ToggleOnlySampFilter() 
        { 
            FilterBySamp(IsOnlySamp); 
        }
        private void ToggleSampCacFilter() 
        { 
            FilterBySampCac(IsSampCac); 
        }
        private void CloseMainWindow()
        {
            _currentOperationCancellationTokenSource?.Cancel();
            _addFavoriteServerDialog?.Close();
            _addGameClientWindow?.Close();
        }
        private async void OnMonitoringServiceChangedAsync() 
        {
            if (SelectedMonitoringService is null)
                return;
            _logger.LogInformation("Selected monitoring service: {NAME}", SelectedMonitoringService);
            await _serverListService.UpdateMonitoringGatewayAsync(SelectedMonitoringService);
            await RefreshServers();
        }
        private void OnClientChanged()
        {
            if (SelectedClient == null || string.IsNullOrWhiteSpace(SelectedClient.Path)) return;
            const string keyPath = @"SOFTWARE\SAMP";
            const string valueName = "gta_sa_exe";
            var path = $"{SelectedClient.Path}\\{SelectedClient.GameExecutableName}";
            try
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\" + keyPath, valueName, path, RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting registry value for SAMP client path");
            }
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
            var serverListViewSource = ServerListView;
            if (serverListViewSource is null)
            {
                _logger.LogError("Internal error. Cannot find DefaultView for ServerList");
                return;
            }

            serverListViewSource.Filter = item =>
            {
                if (item is not ServerMeta server)
                    return false;

                // Фильтр по имени
                if (!UpdateFilterItem("Name",
                    item,
                    server,
                    IsInvertedHostname))
                    return false;

                // Фильтр по режиму
                if (!UpdateFilterItem("Gamemode",
                    item,
                    server,
                    IsInvertedGamemode))
                    return false;

                // Фильтр по языку
                if (!UpdateFilterItem("Language",
                    item,
                    server,
                    IsInvertedLanguage))
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

                // Фильтр по самп
                if (_activeFilters.TryGetValue("IsSamp", out var isSampObj) && isSampObj is bool isSamp)
                {
                    if (server.IsOpenMp == isSamp)
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

            try
            {
                if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == false)
                    System.Windows.Application.Current.Dispatcher.Invoke(() => 
                    serverListViewSource.Refresh());
                else
                    serverListViewSource.Refresh();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing ServerListView after filter change");
            }
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
        private void FilterBySamp(bool check) => SetFilter("IsSamp", check ? true : null);
        private void FilterBySampCac(bool check) => SetFilter("IsSampCac", check ? true : null);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private static bool IsValidNicknameField(string? nickname)
        {
            if (nickname is null)
                return false;
            if (nickname.Length == 0 || nickname.IsWhiteSpace())
                return true;
            return NicknameValidCharsRegex.IsMatch(nickname);
        }
    }

    public enum ServerListMode { All, Favorites, History }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
