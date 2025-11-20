using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using SimpleLauncher.Presentation.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace SimpleLauncher.Presentation
{
    /// <summary>
    /// Логика взаимодействия для AddGameClient.xaml
    /// </summary>
    public partial class AddGameClientWindow : Window
    {
        private readonly ILogger<AddGameClientWindow> _logger;
        private readonly IGameClientService _gameClientService;
        public AddGameClientWindow(ILogger<AddGameClientWindow> logger,
            IGameClientService gameClientService,
            AddGameClientWindowViewModel addGameClientViewModel,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            InitializeComponent();
            DataContext = addGameClientViewModel;
            _gameClientService = gameClientService;
        }
        public async void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _logger.LogInformation("GameClient double clicked");
            if (DataContext is AddGameClientWindowViewModel vm && 
                vm.ClickSelectedGameClientCommand.CanExecute(null))
            {
                var gameClient = _gameClientList.SelectedItem as GameClient;
                if (gameClient is null)
                {
                    _logger.LogError("GameClient is null (ListViewDoubleClick)");
                    return;
                }
                vm.ClickSelectedGameClientCommand.Execute(gameClient);
                _logger.LogInformation("ClickSelectedGameClientCommand executed");
            }
        }

        private async void _removeClientButton_Click(object sender, RoutedEventArgs e)
        {
            var gameClient = _gameClientList.SelectedItem as GameClient;
            
            if (gameClient is null)
            {
                _logger.LogError("GameClient is null (RemoveClientButtonClickEvent)");
                return;
            }
            if (DataContext is AddGameClientWindowViewModel vm && 
                vm.RemoveGameClientCommand.CanExecute(null))
            {
                vm.RemoveGameClientCommand.Execute(gameClient);
            }
        }

        private void _gameClientList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_gameClientList.SelectedItem is not null)
                _removeClientButton.Visibility = Visibility.Visible;
            else
                _removeClientButton.Visibility = Visibility.Hidden;
        }
    }
}
