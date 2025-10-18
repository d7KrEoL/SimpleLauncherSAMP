using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using System.Windows;
using SimpleLauncher.Presentation.ViewModels;

namespace SimpleLauncher.Presentation
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogService _logService;
        private readonly ILogger<MainWindow> _logger;
        private readonly ISampQueryAdapter _sampQuery;
        private readonly IServerListService _serverListService;
        private readonly IConfiguration _configuration;
        private readonly IConfigurationService _configurationService;
        private ServerInfoWindow? _serverInfoWindow;
        private AddFavoriteServerDialog? _addFavoriteServerDialog;

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
            _serverListService = serverList;
            _sampQuery = sampQuery;
            _configuration = configuration;
            _configurationService = configurationService;

            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _serverInfoWindow?.Close();
        }
    }
}