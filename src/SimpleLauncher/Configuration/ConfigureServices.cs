using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleLauncher.Application.Services;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Infrastructure.Game;
using SimpleLauncher.Infrastructure.MonitorAPI.Gateways;
using SimpleLauncher.Infrastructure.SampQuery;
using SimpleLauncher.Presentation;
using SimpleLauncher.Presentation.ViewModels;

namespace SimpleLauncher.Configuration
{
    static class ConfigureServices
    {
        private const string ConfigurationFilePath = "settings.json";
        public static void Configure(IServiceCollection collection)
        {
            AddConfiguration(collection);
            AddInfrastructureServices(collection);
            AddApplicationServices(collection);
            AddLoggingServices(collection);
            AddPresentationServices(collection);
        }
        private static void AddConfiguration(IServiceCollection collection)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(ConfigurationFilePath, 
                    optional: false, 
                    reloadOnChange: true)
                .Build();
            collection.AddSingleton<IConfiguration>(configuration);
        }
        private static void AddInfrastructureServices(IServiceCollection collection)
        {
            collection.AddHttpClient(string.Empty, config =>
            {
                config.DefaultRequestHeaders.Add("User-Agent", "SimpleLauncher/1.0");
                config.DefaultRequestHeaders.Add("Accept", "application/json");
                config.Timeout = TimeSpan.FromSeconds(30);
            });
            collection.AddScoped<IGameProcessManager, GameProcessManager>();
            collection.AddScoped<ISampQueryAdapter, SampQueryAdapter>();
            collection.AddScoped<IMonitoringApiGateway, SAMonitorApiGateway>();
            collection.AddScoped<IMonitoringApiGateway, OpenMpMonigorApiGateway>();
        }
        private static void AddApplicationServices(IServiceCollection collection)
        {
            collection.AddScoped<IConfigurationService>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var configFilePath = ConfigurationFilePath;
                return new ConfigurationService(configuration, configFilePath);
            });
            collection.AddScoped<IServerListService, ServerListService>();
            collection.AddScoped<IGameClientService, GameClientService>();
        }
        private static void AddLoggingServices(IServiceCollection collection)
        {
            if (!collection.Any(x => x.ServiceType == typeof(ILogService)))
                collection.AddSingleton<ILogService, LogService>();
            collection.AddScoped<ILoggerFactory, LoggerFactory>();
        }
        private static void AddPresentationServices(IServiceCollection collection)
        {
            collection.AddTransient<AddGameClientWindowViewModel>();
            collection.AddTransient<MainWindowViewModel>();
            collection.AddTransient<MainWindow>();
            collection.AddTransient<ServerInfoWindow>();
            collection.AddTransient<AddFavoriteServerDialog>();
            collection.AddTransient<GameClientInfoDialog>();
            collection.AddTransient<AddGameClientWindow>();
        }
    }
}
