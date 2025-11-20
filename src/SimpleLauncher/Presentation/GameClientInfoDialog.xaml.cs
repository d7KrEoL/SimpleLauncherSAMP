using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;

namespace SimpleLauncher.Presentation
{
    /// <summary>
    /// Логика взаимодействия для GameClientInfoDialog.xaml
    /// </summary>
    public partial class GameClientInfoDialog : Window
    {
        public delegate void GameClientInfoDialogExitEventArgs(bool okCancel,
            GameClient? client,
            string? legacyClientName = null);
        public string ClientPath { get; private set; }
        private event GameClientInfoDialogExitEventArgs _onDialogExit;
        private readonly ILogger<GameClientInfoDialog> _logger;
        private GameClient? _client;
        public GameClientInfoDialog(ILogger<GameClientInfoDialog> logger)
        {
            _logger = logger;
            InitializeComponent();
        }
        public void ShowDialog(GameClient? client,
            GameClientInfoDialogExitEventArgs? onDialogExit)
        {
            if (onDialogExit is not null)
                _onDialogExit += onDialogExit;
            if (client is null)
                ShowGamePathSelectionDialog();
            else
            { 
                _client = new GameClient(client.Name,
                    client.Version,
                    client.Path,
                    client.ClientExecutableName,
                    client.GameExecutableName);
                _buildName.Text = client.Name;
                _buildVersion.Text = client.Version;
                _gameBuildPath.Text = client.Path;
            }
            var result = ShowDialog();
            if (result is null)
                _logger.LogError("Unexpected error showing dialog");
            SetErrorText("Unexpected error showing dialog");
        }

        private void ExitDialog(bool exitStatus)
        {
            if (_client is not null)
            {
                _onDialogExit.Invoke(exitStatus, _client);
                return;
            }
            var gameExecutable = Infrastructure
                .Game
                .Utils
                .GameFiles
                .GetGameExecutableInDirectory(_gameBuildPath.Text);
            if (gameExecutable is null)
            {
                _logger.LogError("Wrong game path (no game executable file): {path}", _gameBuildPath.Text);
                SetErrorText($"Wrong game path (no game executable file): \"{_gameBuildPath.Text}\"");
                return;
            }
            var clientLibrary = Infrastructure
                .Game
                .Utils
                .GameFiles
                .GetClientLibraryInDirectory(_gameBuildPath.Text);
            if (clientLibrary is null)
            {
                _logger.LogError("Wrong game path (no client library file): {path}", _gameBuildPath.Text);
                SetErrorText($"Wrong game path (no client library file): \"{_gameBuildPath.Text}\"");
                return;
            }
            _client = new GameClient(_buildName.Text,
                _buildVersion.Text,
                _gameBuildPath.Text,
                clientLibrary,
                gameExecutable);
            _onDialogExit.Invoke(exitStatus, _client);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ExitDialog(false);
        }

        private void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            ExitDialog(true);
            Close();
        }

        private void CancelButton_Click_1(object sender, RoutedEventArgs e)
        {
            ExitDialog(false);
            Close();
        }

        private void _buildName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_buildName.Text.Length < 1)
                return;
            _client?.SetName(_buildName.Text);
        }

        private void _buildVersion_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_buildVersion.Text.Length < 1)
                return;
            _client?.SetVersion(_buildVersion.Text);
        }

        private void _gameBuildPath_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => ShowGamePathSelectionDialog();
        private void ShowGamePathSelectionDialog()
        {
            var pathSelectionResult =
                Infrastructure
                .System
                .Utils
                .FileSelectionDialogTools
                .ShowGameFolderSelectionDialog();
            if (!pathSelectionResult.result)
            {
                _logger.LogInformation(pathSelectionResult.pathOrError);
                SetErrorText(pathSelectionResult.pathOrError ?? "Unexpected error");
                return;
            }
            if (pathSelectionResult.pathOrError is null)
            {
                _logger.LogError("Unexpected error while selecting game path (path is null)");
                SetErrorText("Unexpected error while selecting game path (path is null)");
                return;
            }
            var gameExecutablePath = Infrastructure
                .Game
                .Utils
                .GameFiles
                .GetGameExecutableInDirectory(pathSelectionResult.pathOrError);
            if (gameExecutablePath is null)
            {
                _logger.LogError("Wrong game path. Cannot find game executable");
                SetErrorText("Wrong game path. Cannot find game executable");
                return;
            }
            var clientLibraryPath = Infrastructure
                .Game
                .Utils
                .GameFiles
                .GetClientLibraryInDirectory(pathSelectionResult.pathOrError);
            if (clientLibraryPath is null)
            {
                _logger.LogError("Wrong game path. Cannot find client library");
                SetErrorText("Wrong game path. Cannot find client library");
                return;
            }
            ClientPath = pathSelectionResult.pathOrError;
            _gameBuildPath.Text = ClientPath;
            ResetErrors();
            if (_client is null)
            {
                _client = new GameClient(_buildName.Text,
                    _buildVersion.Text,
                    _gameBuildPath.Text,
                    clientLibraryPath,
                    gameExecutablePath);
                return;
            }
            _client.SetPath(pathSelectionResult.pathOrError);
        }
        private void SetErrorText(string text)
        {
            _errorTextBlock.Text = text;
            _enterButton.IsEnabled = false;
        }
        private void ResetErrors()
        {
            _errorTextBlock.Text = string.Empty;
            _enterButton.IsEnabled = true;
        }
    }
}
