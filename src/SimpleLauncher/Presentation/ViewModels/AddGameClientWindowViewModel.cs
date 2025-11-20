using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleLauncher.Application.Extensions;
using SimpleLauncher.Application.Services;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SimpleLauncher.Presentation.ViewModels
{
    public class AddGameClientWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<GameClient> ClientList { get; }
        private readonly ILogger<AddGameClientWindowViewModel> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly IGameClientService _gameClientService;
        private readonly IServiceProvider _serviceProvider;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private GameClient? _selectedClient;
        public GameClient? SelectedClient
        {
            get => _selectedClient;
            set { _selectedClient = value; OnPropertyChanged(); }
        }

        public ICommand AddGameClientCommand { get; }
        public ICommand RemoveGameClientCommand { get; }
        public ICommand UpdateGameClientCommand { get; }
        public ICommand ChangeSelectedGameClientCommand { get; }
        public ICommand ShowGameClientFolderSelectionDialogCommand { get; }
        public ICommand ClickSelectedGameClientCommand { get; }

        public AddGameClientWindowViewModel(ILogger<AddGameClientWindowViewModel> logger,
            IConfigurationService configurationService,
            IGameClientService gameClientService,
            IServiceProvider serviceProvider)//ObservableCollection<GameClient> clientList
        {
            _logger = logger;
            _configurationService = configurationService;
            _gameClientService = gameClientService;
            _serviceProvider = serviceProvider;
            ClientList = (_gameClientService as GameClientService)?.Clients
                ?? new ObservableCollection<GameClient>();

            AddGameClientCommand = new RelayCommand(param => AddGameClientAsync(param));
            RemoveGameClientCommand = new RelayCommand(param => RemoveGameClientAsync(param));
            UpdateGameClientCommand = new RelayCommand(param => UpdateGameClientAsync(param));
            ChangeSelectedGameClientCommand = new RelayCommand(param => ChangeSelectedGameClientAsync(param));
            ClickSelectedGameClientCommand = new RelayCommand(param => ClickSelectedGameClientAsync(param));
            ShowGameClientFolderSelectionDialogCommand = new RelayCommand(_ => ShowGameClientFolderSelectionDialogAsync());
        }

        private async void AddGameClientAsync(object? param)
        {
            var gameClient = param as GameClient;
            if (gameClient is null)
                return;
            if (!await _gameClientService.AddGameClientAsync(gameClient))
                _logger.LogError("Error adding game client {name}", gameClient.Name);
        }
        private async void RemoveGameClientAsync(object? param)
        {
            var gameClient = param as GameClient;
            if (gameClient is null)
                return;
            if (!ClientList.Remove(gameClient))
                _logger.LogError("Error removing game client {name}", gameClient.Name);
            if (!_configurationService.DeleteGameClient(gameClient.Name))
                _logger.LogError("Error removing game client {name} from configuration", gameClient.Name);
            _logger.LogInformation("game client was successfully removed: {name}", gameClient.Name);
        }
        private async void UpdateGameClientAsync(object? param)
        {
            var gameClient = param as GameClient;
            if (gameClient is null) 
                return;
            throw new NotImplementedException();
        }
        private async void ChangeSelectedGameClientAsync(object? param)
        {
            var gameClient = param as GameClient;
            if (gameClient is null)
                return;
            throw new NotImplementedException();
        }
        private async void ClickSelectedGameClientAsync(object? param)
        {
            var gameClient = param as GameClient;
            if (gameClient is null)
                return;
            var infoDialog = _serviceProvider
                .GetRequiredService<GameClientInfoDialog>();
            infoDialog.ShowDialog(gameClient,
                OnGameClientInfoDialogCloseAsync);
        }
        private async void ShowGameClientFolderSelectionDialogAsync(GameClient? selectedClient = null)
        {
            var infoDialog = _serviceProvider
                .GetRequiredService<GameClientInfoDialog>();
            infoDialog.ShowDialog(selectedClient,
                OnGameClientInfoDialogCloseAsync);
        }
        public async void OnGameClientInfoDialogCloseAsync(bool isOk, 
            GameClient? client, 
            string? legacyClientName = null)
        {
            if (!isOk)
                return;
            if (client is null)
                return;
            if (client.IsBadPath())
                return;
            if (!await _gameClientService.AddGameClientAsync(client))
                _logger.LogInformation("Client: {name} is already in client list (path: {clientPath})",
                    client.Name,
                    client.Path);
            else
                await RefreshClientListConfig(legacyClientName, client);
        }
        private async Task RefreshClientListConfig(string? legacyClientName, GameClient client)
        {
            if (string.IsNullOrEmpty(legacyClientName))
                _configurationService.EditGameClient(client.Name, client);
            else
                _configurationService.EditGameClient(legacyClientName,
                        client);
        }
    }
}
