using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Numerics;
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
        private string _playerNickname = "Unnamed";
        private CancellationTokenSource? _pingCancellationTokenSource;
        private ObservableCollection<PlayerMeta> _players = new ObservableCollection<PlayerMeta>();
        private ServerMeta _serverInfo;
        private List<PlayerMeta> _playersInfo;
        private Process? _gameProcess;
        private Point[] _printPoints = new Point[DrawnablePointsCount];
        public ServerInfoWindow(ILogService logService, 
            ILogger<ServerInfoWindow> logger, 
            IServerListService serverListService,
            IConfigurationService configurationService)
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
        }
        public async Task Configure(ServerMeta serverInfo,
            List<PlayerMeta> playersInfo,
            CancellationToken cancellationToken,
            string playerNickname)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _playerNickname = playerNickname;
            await UpdateInfoAsync(serverInfo, playersInfo);
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
            if (_gameProcess is not null &&
                !_gameProcess.CloseMainWindow())
                _gameProcess.Kill();
        }
        /*
            -c rcon_password
            -n player name
            -h server ip
            -p server port
            -z server password
            -d debug */
        private void _connectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clientPath = FindSampPathInRegistry();
                string args;
                if (string.IsNullOrWhiteSpace(_serverPasswordInput.Password))
                    args = $"{_serverInfo.IpAddress} -n{_playerNickname}";
                else
                    args = $"{_serverInfo.IpAddress} -n{_playerNickname} -z{_serverPasswordInput.Password}";
                _logger.LogInformation("Starting game with {ARGS}", args);
                _gameProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = $"{Path.GetFileName(clientPath)}",
                    Arguments = args,
                    WorkingDirectory = Path.GetDirectoryName(clientPath),
                    UseShellExecute = true
                });
                _logger.LogInformation("Starting game: {GAME}", 
                    _gameProcess?.MainWindowTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot start samp instance");
            }
            _configurationService.AddValueToArray("ServerList:LastConnected", _serverInfo.IpAddress);
        }

        private void _saveButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void _closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private static string FindSampPathInRegistry()
        {
            const string registryPath = @"SOFTWARE\SAMP";
            const string valueName = "gta_sa_exe";

            using var key = Registry.CurrentUser.OpenSubKey(registryPath);
            if (key is null)
                throw new KeyNotFoundException("Cannot find installed samp instance");
            var gtaPath = key.GetValue(valueName) as string;
            if (gtaPath is null)
                throw new FileNotFoundException("Cannot find gta_sa_exe registry key");
            var gameDirectory = Path.GetDirectoryName(gtaPath);
            if (string.IsNullOrEmpty(gameDirectory))
                throw new DirectoryNotFoundException("Cannot find gta game directory");
            var clientPath = Path.Combine(gameDirectory, "samp.exe");
            if (!File.Exists(clientPath))
            {
                string[] exeFiles = Directory.GetFiles(gameDirectory, "*.exe");

                clientPath = exeFiles?
                    .Where(file => file
                        .StartsWith("samp", StringComparison.OrdinalIgnoreCase))
                    .OfType<string>()?
                    .Take(1)?
                    .ToArray()[0] ?? 
                    throw new FileNotFoundException("Cannot find samp.exe in samp directory");
            }
            return clientPath;
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
                            _logger.LogInformation("Ping: {PONG}", (uint)reply.RoundtripTime);
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
