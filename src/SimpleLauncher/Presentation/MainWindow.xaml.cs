using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using SimpleLauncher.Presentation.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace SimpleLauncher.Presentation
{
    public partial class MainWindow : Window
    {
        private const int MaxLogMessages = 100;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogService _logService;
        private readonly ILogger<MainWindow> _logger;
        private ServerInfoWindow? _serverInfoWindow;
        private AddFavoriteServerDialog? _addFavoriteServerDialog;
        private CancellationTokenSource? _serverInfoCancellationTokenSource;
        private ICollectionView? _serverListViewSource;
        public ICollectionView? ServerListViewSource
        {
            get => _serverListViewSource;
            set => _serverListViewSource = value;
        }

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

            InitializeComponent();
            _versionLabel.Text = $"Version: " +
                $"{Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion}";
            DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            Loaded += MainWindow_Loaded;
            ((INotifyCollectionChanged)_logBox.Items).CollectionChanged += _logBox_ItemsChanged;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //_serverListViewSource = (CollectionViewSource)FindName("ServerList"); 
            _serverListViewSource = CollectionViewSource.GetDefaultView(_serverList.ItemsSource);
            if (DataContext is MainWindowViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm && vm.CloseMainWindowCommand.CanExecute(null))
            {
                _serverInfoCancellationTokenSource?.Cancel();
                vm.CloseMainWindowCommand.Execute(null);
                _serverInfoWindow?.Close();
                _addFavoriteServerDialog?.Close();
            }
        }
        private async void _addFavoriteServerButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainWindowViewModel;
            if (vm is null)
            {
                _logger.LogError("MainWindowViewModel is null in _addFavoriteServerButton_Click");
                return;
            }
            if (_addFavoriteServerDialog is not null)
            {
                _addFavoriteServerDialog.Close();
                _addFavoriteServerDialog = null;
            }
            _addFavoriteServerDialog = _serviceProvider.GetRequiredService<AddFavoriteServerDialog>();
            _addFavoriteServerDialog.Owner = this;
            _addFavoriteServerDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            await _addFavoriteServerDialog.ShowAddServerDialog(vm.OnAddServerToFavorites);
        }
        private async void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is ServerMeta server)
            {
                var vm = DataContext as MainWindowViewModel;
                if (vm is null)
                {
                    _logger.LogError("MainWindowViewModel is null in ListViewItem_MouseDoubleClick");
                    return;
                }
                if (_serverInfoWindow is not null)
                {
                    await CloseServerInfoWindowToken();
                    _serverInfoWindow.Close();
                    _serverInfoWindow = null;
                }
                await RecreateServerInfoWindowToken();
                vm.ServerDoubleClickCommand.Execute(server);
                _serverInfoWindow = _serviceProvider.GetRequiredService<ServerInfoWindow>();
                _serverInfoWindow.Owner = this;
                _serverInfoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                _serverInfoWindow.Show();
                
                var serverInfo = server;
                var cancellationToken = _serverInfoCancellationTokenSource?.Token;
                if (cancellationToken is null)
                {
                    _serverInfoWindow?.Close();
                    return;
                }
                var players = serverInfo.PlayersCount switch
                {
                        0 => new List<PlayerMeta> { PlayerMeta.CreateEmpty("No players online.")},
                        > 70 => new List<PlayerMeta> { PlayerMeta.CreateEmpty("Too many players...")},
                        _ => new List<PlayerMeta> { PlayerMeta.CreateEmpty("Loading...") }
                };
                try
                {
                    _ = _serverInfoWindow?.Configure(serverInfo,
                        players,
                        cancellationToken ?? new CancellationToken(true),
                        _nicknameInput.Text,
                        vm.OnAddServerToFavorites,
                        vm.OnAddServerToHistory);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogDebug(ex, "Operation in serverInfo window was cancelled");
                    _serverInfoWindow?.Close();
                }
            }
        }

        private async Task CloseServerInfoWindowToken()
        {
            if (_serverInfoCancellationTokenSource is null)
                return;
            await _serverInfoCancellationTokenSource.CancelAsync();
            _serverInfoCancellationTokenSource.Dispose();
        }
        
        private async Task RecreateServerInfoWindowToken()
        {
            try
            {
                if (_serverInfoCancellationTokenSource?.Token is not null)
                    await _serverInfoCancellationTokenSource.CancelAsync();
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogDebug(ex, "CancellationTokenSource was already disposed");
            }
            _serverInfoCancellationTokenSource = new CancellationTokenSource();
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
                _addGameBuild.Visibility = Visibility.Collapsed;
                _hideExtraMenuButton.Content = "↓";
            }
            else
            {
                _extraPanel.Height = new GridLength(ExtraMenuGridHeight);
                _nicknameInput.Visibility = Visibility.Visible;
                _nickNameTextBlock.Visibility = Visibility.Visible;
                _addFavoritesButton.Visibility = Visibility.Visible;
                _addGameBuild.Visibility = Visibility.Visible;
                _hideExtraMenuButton.Content = "↑";
            }
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
                        _logService.LogEntries.RemoveAt(i);
                });
            }
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
        private void RefreshFilteredSecverList()
        {

        }

        private void _addFavoritesButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}