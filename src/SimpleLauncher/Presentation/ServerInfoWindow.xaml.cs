using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using SimpleLauncher.Infrastructure.Game.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SimpleLauncher.Presentation
{
    public partial class ServerInfoWindow : Window
    {
        private const int DrawnablePointsCount = 30;
        private const double PlayerListCompactModeThreshold = 240;
        private readonly ILogService _logService;
        private readonly ILogger<ServerInfoWindow> _logger;
        private readonly IServerListService _serverListService;
        private readonly IConfigurationService _configurationService;
        private readonly IGameProcessManager _gameProcessManager;
        private event AddFavoriteServerEventHandler? _addFavoritesEvent;
        private event AddHistoryServerEventHandler? _addHistoryEvent;
        private string _playerNickname = "Unnamed";
        private CancellationTokenSource? _pingCancellationTokenSource;
        private CancellationTokenSource? _launchGameCancellationTokenSource;
        private ObservableCollection<PlayerMeta> _players = new ObservableCollection<PlayerMeta>();
        private ServerMeta _serverInfo;
        private List<PlayerMeta> _playersInfo;
        private Process? _gameProcess;
        private Point[] _printPoints = new Point[DrawnablePointsCount];
        public ServerInfoWindow(ILogService logService, 
            ILogger<ServerInfoWindow> logger, 
            IServerListService serverListService,
            IConfigurationService configurationService,
            IGameProcessManager gameProcessManager)
        {
            _logService = logService;
            _logger = logger;
            _serverListService = serverListService;
            _players.Add(new PlayerMeta(0, "Loading...", 0, 0));
            _ = ServerPingCanvasInit();
            ServerPlayersListView_SizeChanged(_serverPlayersListView, null);
            InitializeComponent();
            _serverPlayersListView.ItemsSource = _players;
            _configurationService = configurationService;
            _gameProcessManager = gameProcessManager;
        }
        public async Task Configure(ServerMeta serverInfo,
            List<PlayerMeta>? playersInfo,
            CancellationToken cancellationToken,
            string playerNickname,
            AddFavoriteServerEventHandler? onAddFavoriteServer,
            AddHistoryServerEventHandler? onAddHistoryServer)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _playerNickname = playerNickname;
            await UpdateInfoAsync(serverInfo, playersInfo ?? new List<PlayerMeta>());
            await Task.Run(() =>
            {
                if (_pingCancellationTokenSource is null ||
                    _pingCancellationTokenSource.Token.IsCancellationRequested)
                {
                    _pingCancellationTokenSource?.Cancel();
                    _pingCancellationTokenSource?.Dispose();
                    _pingCancellationTokenSource = new CancellationTokenSource();
                }
                _ = ServerPingUpdater(_pingCancellationTokenSource.Token);
            });
            _addFavoritesEvent += onAddFavoriteServer;
            _addHistoryEvent += onAddHistoryServer;
        }
        public async Task UpdateNickname(string newNickname)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                _playerNickname = newNickname;
            });
        }
        private async Task UpdateInfoAsync(ServerMeta serverInfo,
            List<PlayerMeta> playersInfo)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var selectedValue = _serverPlayersListView.SelectedIndex;
                _serverInfo = serverInfo;
                _playersInfo = playersInfo;
                _serverNameLabel.Content = serverInfo.Name;
                _serverIpLabel.Content = serverInfo.IpAddress;
                _serverOnlineLabel.Content = $"Online: {serverInfo.PlayersCount}/{serverInfo.MaxPlaeyers}";
                _serverPingLabel.Content = $"Ping: {serverInfo.Ping}";
                _serverLanguageLabel.Content = $"Language: {serverInfo.Language}";
                _serverModeLabel.Content = serverInfo.Gamemode;

                _players.Clear();
                foreach (var player in playersInfo)
                {
                    _players.Add(player);
                }
                if (!_players.Any())
                    _players.Add(new PlayerMeta(0, "No players online", 0, 0));
                _serverPlayersListView.SelectedIndex = selectedValue;
                var listViewMessage = _serverPlayersListView.Items[0] as PlayerMeta;
                if (_serverPingLabel.Content.Equals("Ping: 0") &&
                    listViewMessage is not null &&
                    listViewMessage.Name.Equals("Cannot connect"))
                {
                    _connectButton.IsEnabled = false;
                }
                else
                {
                    _connectButton.IsEnabled = true;
                }
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _pingCancellationTokenSource?.Cancel();
            _pingCancellationTokenSource?.Dispose();
            GameTermination();
            _serverInfo = ServerMeta.CreateUnknown("Loading...", "Loading...", "Loading...");
            _players.Clear();
            _gameProcess = null;
        }
        private async void GameTermination()
        {
            if (_gameProcess is not null) 
                await GameProcess.TerminateProcessAsync(_gameProcess);
        }
        /*
            -c rcon_password
            -n player name
            -h server ip
            -p server port
            -z server password
            -d debug 
        TODO replace connection function with something like this:
        https://github.com/BigETI/SAMPLauncherNET/blob/master/SAMPLauncherNET/Source/SAMPLauncherNET/Core/SAMP.cs
        */
        private void _connectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _launchGameCancellationTokenSource?.Cancel();
                _launchGameCancellationTokenSource?.Dispose();
                _launchGameCancellationTokenSource = new CancellationTokenSource();
                var clientPath = 
                    GameFiles.GetClientLibraryPathFromExecutablePath(SystemRegistry.FindSampPathInRegistry());
                if (clientPath is null)
                {
                    _logger.LogError("Cannot find SAMP client library in registry path");
                    return;
                }
                var gamePath = GameFiles.GetGameExecutablePathFromClientPath(clientPath);
                if (gamePath is null)
                {
                    _logger.LogError("Cannot find GTA:SA executable in root directory of SAMP client: {CLIENTPATH}", 
                        clientPath);
                    return;
                }

                var ipPort = _serverInfo.IpAddress.Split(':');
                string args = $"-h {ipPort[0]} -p {ipPort[1]} -n {_playerNickname}";
                if (!string.IsNullOrWhiteSpace(_serverRconInput.Password))
                    args += $" -c {_serverRconInput.Password}";
                if (!string.IsNullOrWhiteSpace(_serverPasswordInput.Password))
                    args += $" -z {_serverPasswordInput.Password}";
                _logger.LogInformation("Starting game with {ARGS}", args);

                _gameProcessManager.StartAndConnectAsync(gamePath, 
                    clientPath, 
                    args, 
                    _serverInfo.IpAddress, 
                    new List<GameAddon>(),
                    _launchGameCancellationTokenSource.Token);
            }
            catch(DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, "Cannot find game directory");
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Cannot find game executable (wrong samp directory)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot launch game instance");
            }
            var isAdded = _configurationService
                .AddValueToArray("ServerList:LastConnected", _serverInfo.IpAddress);
            _logger.LogInformation("Server {INFO} was {ISADDED} to last connected serverlist", 
                _serverInfo.IpAddress,
                isAdded ? "added" : "not added");
        }

        private void _saveHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            _addHistoryEvent?.Invoke(sender, 
                _serverInfo.IpAddress, 
                AddFavoriteOrHistoryOperationResult.Success);
        }

        private void _saveFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            _addFavoritesEvent?.Invoke(sender,
                _serverInfo.IpAddress,
                AddFavoriteOrHistoryOperationResult.Success);
        }

        private void _closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private async Task ServerPingCanvasInit()
        {
            for (int i = 0; i < DrawnablePointsCount; i++)
                _printPoints[i] = new Point(0, i * 10);
        }
        private async Task DrawServerPing(uint ping)
        {
            await DrawPingAxis();
            await Dispatcher.InvokeAsync(() =>
            {
                _serverPingCanvas.Children.Clear();

                var polyline = new System.Windows.Shapes.Polyline
                {
                    Stroke = Brushes.Orange,
                    StrokeThickness = 2,
                    StrokeLineJoin = PenLineJoin.Round
                };
                for (int i = 0; i < DrawnablePointsCount - 1; i++)
                {
                    _printPoints[i] = _printPoints[i + 1];
                    polyline.Points.Add(_printPoints[i]);

                }
                _printPoints[DrawnablePointsCount - 1] = new Point(30, ping);
                polyline.Points.Add(_printPoints[DrawnablePointsCount - 1]);
                _serverPingCanvas.Children.Add(polyline);
            });
        }
        private async Task DrawPingAxis()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var xAxis = new System.Windows.Shapes.Line
                {
                    X1 = 0,
                    Y1 = _serverPingCanvas.ActualHeight,
                    X2 = _serverPingCanvas.ActualWidth,
                    Y2 = _serverPingCanvas.ActualHeight,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };

                var yAxis = new System.Windows.Shapes.Line
                {
                    X1 = 0,
                    Y1 = 0,
                    X2 = 0,
                    Y2 = _serverPingCanvas.ActualHeight,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };

                _serverPingCanvas.Children.Add(xAxis);
                _serverPingCanvas.Children.Add(yAxis);
            });
        }
        private async Task ServerPingUpdater(CancellationToken stoppingToken)
        {
            const int InfoUpdateDelayMilliseconds = 3000;
            const int PingTimeoutMilliseconds = 1000;
            bool infoPlayerList = false;
            var stopQueryTokenSource = new CancellationTokenSource();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await stopQueryTokenSource.CancelAsync();
                    stopQueryTokenSource.Dispose();
                    stopQueryTokenSource = new CancellationTokenSource();
                    if (infoPlayerList)
                        _serverInfo =
                            await _serverListService.GetServerInfoAsync(_serverInfo.IpAddress,
                            stopQueryTokenSource.Token) ?? _serverInfo;
                    else
                        _playersInfo =
                            await _serverListService.GetServerPlayersAsync(_serverInfo.IpAddress,
                            stopQueryTokenSource.Token) ?? _playersInfo;
                    await Task.Delay(TimeSpan.FromMilliseconds(PingTimeoutMilliseconds / 3));
                    uint pingValue = 0;
                    using (var ping = new Ping())
                    {
                        var reply = await ping.SendPingAsync(_serverInfo.IpAddress.Split(':')[0],
                            PingTimeoutMilliseconds);
                        if (!reply.Status.Equals(IPStatus.Success))
                        {
                            pingValue = 0;
                            _logger.LogError("Cannot ping: {PONG}", reply.Status.ToString());
                        }
                        else
                        {
                            pingValue = (uint)reply.RoundtripTime;
                            _logger.LogTrace("Ping: {PONG}", (uint)reply.RoundtripTime);
                        }
                    }
                    _serverInfo.UpdatePing(pingValue);

                    await UpdateInfoAsync(_serverInfo, _playersInfo);
                    await DrawServerPing(_serverInfo.Ping);
                    await Task.Delay(TimeSpan.FromMilliseconds(InfoUpdateDelayMilliseconds));
                }
                catch(OperationCanceledException ex)
                {
                    _logger.LogWarning(ex, "Ping was cancelled");
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
        }

        private void ServerPlayersListView_SizeChanged(object sender, SizeChangedEventArgs? e)
        {
            if (sender is ListView listView && listView.View is GridView gridView)
            {
                double availableWidth = listView.ActualWidth - 25;

                bool showId = availableWidth > PlayerListCompactModeThreshold / 1.5;    
                bool showScore = availableWidth > PlayerListCompactModeThreshold / 1.25;  
                bool showPing = availableWidth > PlayerListCompactModeThreshold;  

                NameColumn.Width = CalculateNameColumnWidth(availableWidth, showId, showScore, showPing);

                IdColumn.Width = showId ? 40 : 0;
                ScoreColumn.Width = showScore ? 80 : 0;
                PingColumn.Width = showPing ? 60 : 0;
            }
        }
        private static double CalculateNameColumnWidth(double availableWidth, bool showId, bool showScore, bool showPing)
        {
            double usedWidth = 0;

            if (showId) usedWidth += 40;
            if (showScore) usedWidth += 80;
            if (showPing) usedWidth += 60;

            double nameWidth = availableWidth - usedWidth;
            return Math.Max(120, Math.Min(300, nameWidth));
        }

        private void _serverPlayersListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Clipboard.SetText(_players[_serverPlayersListView.SelectedIndex].Name);
        }

        private void _serverIpLabel_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var text = _serverIpLabel.Content.ToString();
            if (text is not null)
                Clipboard.SetText(text);
        }
    }
}
