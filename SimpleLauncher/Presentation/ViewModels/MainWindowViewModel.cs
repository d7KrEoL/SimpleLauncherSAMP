using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SimpleLauncher.Presentation.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ServerMeta> ServerList { get; } = new();
        public ObservableCollection<string> LogEntries { get; } = new();
        public ObservableCollection<IMonitoringApiGateway> MonitoringServices { get; } = new();
        public ObservableCollection<GameClient> ClientList { get; } = new();

        private string _nickname = "Nickname";
        public string Nickname
        {
            get => _nickname;
            set { _nickname = value; OnPropertyChanged(); }
        }

        private IMonitoringApiGateway? _selectedMonitoringService;
        public IMonitoringApiGateway? SelectedMonitoringService
        {
            get => _selectedMonitoringService;
            set { _selectedMonitoringService = value; OnPropertyChanged(); OnMonitoringServiceChanged(); }
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
            set { _hostnameFilter = value; OnPropertyChanged(); UpdateServerListFilter(); }
        }
        private string _gamemodeFilter = "";
        public string GamemodeFilter
        {
            get => _gamemodeFilter;
            set { _gamemodeFilter = value; OnPropertyChanged(); UpdateServerListFilter(); }
        }
        private string _languageFilter = "";
        public string LanguageFilter
        {
            get => _languageFilter;
            set { _languageFilter = value; OnPropertyChanged(); UpdateServerListFilter(); }
        }
        public bool IsInvertedHostname { get; set; }
        public bool IsInvertedGamemode { get; set; }
        public bool IsInvertedLanguage { get; set; }

        // Дополнительные фильтры
        public bool IsOnlyLagcomp { get; set; }
        public bool IsOnlyLagshot { get; set; }
        public bool IsNotEmpty { get; set; }
        public bool IsNotFull { get; set; }
        public bool IsNoPassword { get; set; }
        public bool IsOnlyOpenMp { get; set; }
        public bool IsOnlySamp { get; set; }
        public bool IsSampCac { get; set; }

        private string? _currentSortColumn;
        private ListSortDirection _currentSortDirection = ListSortDirection.Ascending;
        private ServerListMode _currentServerListMode = ServerListMode.All;

        public ICommand RefreshCommand { get; }
        public ICommand AddFavoriteCommand { get; }
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

        public MainWindowViewModel()
        {
            RefreshCommand = new RelayCommand(_ => RefreshServers());
            AddFavoriteCommand = new RelayCommand(_ => AddFavoriteServer());
            AddHistoryCommand = new RelayCommand(_ => AddHistoryServer());
            SwitchFavoritesCommand = new RelayCommand(_ => SwitchFavorites());
            SwitchHistoryCommand = new RelayCommand(_ => SwitchHistory());
            SwitchAllCommand = new RelayCommand(_ => SwitchAll());
            ClearLogCommand = new RelayCommand(_ => ClearLog());
            SortCommand = new RelayCommand(param => SortBy(param?.ToString()));
            ServerDoubleClickCommand = new RelayCommand(param => OnServerDoubleClick(param));
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
        }

        private void RefreshServers() { /* Реализация загрузки серверов */ }
        private void AddFavoriteServer() { /* Реализация добавления */ }
        private void AddHistoryServer() { /* Реализация добавления */ }
        private void SwitchFavorites() { _currentServerListMode = ServerListMode.Favorites; RefreshServers(); }
        private void SwitchHistory() { _currentServerListMode = ServerListMode.History; RefreshServers(); }
        private void SwitchAll() { _currentServerListMode = ServerListMode.All; RefreshServers(); }
        private void ClearLog() { LogEntries.Clear(); }
        private void SortBy(string? columnName) { /* Реализация сортировки */ }
        private void OnServerDoubleClick(object? param) { /* Открытие окна информации о сервере */ }
        private void HideExtraMenu() { /* Скрытие/отображение панели */ }
        private void ToggleLagcompFilter() { IsOnlyLagcomp = !IsOnlyLagcomp; UpdateServerListFilter(); }
        private void ToggleLagshotFilter() { IsOnlyLagshot = !IsOnlyLagshot; UpdateServerListFilter(); }
        private void ToggleNotEmptyFilter() { IsNotEmpty = !IsNotEmpty; UpdateServerListFilter(); }
        private void ToggleNotFullFilter() { IsNotFull = !IsNotFull; UpdateServerListFilter(); }
        private void ToggleNoPasswordFilter() { IsNoPassword = !IsNoPassword; UpdateServerListFilter(); }
        private void ToggleOnlyOpenMpFilter() { IsOnlyOpenMp = !IsOnlyOpenMp; UpdateServerListFilter(); }
        private void ToggleOnlySampFilter() { IsOnlySamp = !IsOnlySamp; UpdateServerListFilter(); }
        private void ToggleSampCacFilter() { IsSampCac = !IsSampCac; UpdateServerListFilter(); }

        private void OnMonitoringServiceChanged() { /* Реализация смены мониторинга */ }
        private void OnClientChanged()
        {
            if (SelectedClient == null || string.IsNullOrWhiteSpace(SelectedClient.Path)) return;
            const string keyPath = @"SOFTWARE\SAMP";
            const string valueName = "gta_sa_exe";
            try
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\" + keyPath, valueName, SelectedClient.Path, RegistryValueKind.String);
            }
            catch { }
        }

        private void UpdateServerListFilter() { /* Реализация фильтрации */ }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
