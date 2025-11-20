using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Logging;
using SimpleLauncher.Presentation;
using SimpleLauncher.Application.Services;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace SimpleLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (!Debugger.IsAttached && !IsRunningAsAdmin())
                RestartAsAdmin();

            ServiceProvider serviceProvider;
            var serviceCollection = new ServiceCollection();
            Configuration.ConfigureServices.Configure(serviceCollection);
            var logService = new LogService();
            serviceCollection.AddSingleton<ILogService>(logService);
            serviceCollection.AddLogging(builder =>
            {
                builder.AddProvider(new ListBoxLoggerProvider(
                    logService.LogEntries));
                builder.SetMinimumLevel(LogLevel.Trace);
            });
            serviceCollection.AddTransient<SimpleLauncher.Presentation.ViewModels.MainWindowViewModel>();
            serviceProvider = serviceCollection.BuildServiceProvider();

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private static bool IsRunningAsAdmin()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private static void RestartAsAdmin()
        {
            var processInfo = new ProcessStartInfo
            {
                Verb = "runas",
                FileName = Process.GetCurrentProcess()?.MainModule?.FileName,
                UseShellExecute = true
            };

            try
            {
                Process.Start(processInfo);
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось перезапустить с правами администратора: {ex.Message}");
            }
        }
    }

}
