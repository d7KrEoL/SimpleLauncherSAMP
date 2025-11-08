using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Windows;
using static SimpleLauncher.Presentation.MainWindow;

namespace SimpleLauncher.Presentation
{
    public partial class AddFavoriteServerDialog : Window
    {
        private const string StatusWrongIpAddress = "Invalid ip addres!";
        private const string StatusServerAlreadyExists = "This server is already added to your favorites list";
        private const string FavoritesSectionName = "ServerList:Favorites";
        private readonly ILogger<AddFavoriteServerDialog> _logger;
        private readonly IConfiguration _configuration;
        private event AddFavoriteServerEventHandler? _onDialogClosed;
        public AddFavoriteServerDialog(ILogger<AddFavoriteServerDialog> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            InitializeComponent();
        }
        public async Task ShowAddServerDialog(AddFavoriteServerEventHandler onDialogClosed)
        {
            _onDialogClosed += onDialogClosed;
            Show();
        }
        private bool ValidateIpAddress(string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;
            if (!ipAddress.Contains(':'))
                return false;
            if (!IPEndPoint.TryParse(ipAddress.Remove(ipAddress.IndexOf(':')), out IPEndPoint? localEP))
                return false;
            if (localEP is null)
                return false;
            return true;
        }
        private void SetStatus(string statusMessage, 
            AddFavoriteOrHistoryOperationResult operationCode)
        {
            if (string.IsNullOrWhiteSpace(statusMessage))
            {
                _statusTextBlock.Visibility = Visibility.Hidden;
                return;
            }
            _statusTextBlock.Visibility = Visibility.Visible;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_ipAddressTextBox.Text) ||
                !ValidateIpAddress(_ipAddressTextBox.Text))
            {
                SetStatus(StatusWrongIpAddress, 
                    AddFavoriteOrHistoryOperationResult.FailedBadAddress);
                return;
            }
            if (_configuration[FavoritesSectionName]?.Contains(_ipAddressTextBox.Text ?? string.Empty) ?? false)
            {
                SetStatus(StatusServerAlreadyExists, 
                    AddFavoriteOrHistoryOperationResult.FailedAlreadyExist);
                return;
            }
            _onDialogClosed?.Invoke(this, 
                _ipAddressTextBox.Text ?? string.Empty,
                AddFavoriteOrHistoryOperationResult.Success);
            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _onDialogClosed?.Invoke(this, _ipAddressTextBox.Text, 
                AddFavoriteOrHistoryOperationResult.Cancelled);
            Close();
        }
    }
}
