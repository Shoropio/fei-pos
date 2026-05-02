using System;
using System.Windows;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FeiPos.Infrastructure.Persistence;
using FeiPos.Presentation.ViewModels;
using FeiPos.Application.Interfaces;
using FeiPos.Infrastructure.Services;
using ModernWpf;

namespace FeiPos.Presentation
{
    public partial class App : System.Windows.Application
    {
        private IServiceProvider _serviceProvider = null!;

        public App()
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;

            // Captura de excepciones globales
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            try
            {
                ServiceCollection services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();
            }
            catch (Exception ex)
            {
                File.WriteAllText("critical_error.txt", ex.ToString());
                MessageBox.Show($"Error crítico en DI: {ex.Message}");
            }
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("Data Source=feipos.db"));

            services.AddSingleton<ConfigurationService>();
            services.AddScoped<AuthService>();
            services.AddHttpClient<IHaciendaService, HaciendaService>();
            services.AddHttpClient<HaciendaIdentityService>();
            services.AddSingleton<EscPosPrinterService>();
            services.AddTransient<SalesViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<QueueViewModel>();
            services.AddTransient<InventoryViewModel>();
            services.AddTransient<CustomerViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<SalesHistoryViewModel>();
            services.AddTransient<OpenOrdersViewModel>();
            services.AddTransient<CashDrawerViewModel>();
            services.AddTransient<DayCloseViewModel>();
            services.AddTransient<UsersViewModel>();
            services.AddTransient<CreditAccountsViewModel>();
            services.AddTransient<FiscalInvoicesViewModel>();
            services.AddTransient<BackupsViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<Views.LoginWindow>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<FeiPos.Infrastructure.BackgroundServices.HaciendaQueueWorker>();
            services.AddLogging();
            
            // Importante: Registrar el propio IServiceProvider para la navegación
            services.AddSingleton<IServiceProvider>(sp => sp);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown;

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    DbInitializer.Initialize(dbContext);
                }

                // Iniciar el trabajador de la cola de Hacienda
                var worker = _serviceProvider.GetRequiredService<FeiPos.Infrastructure.BackgroundServices.HaciendaQueueWorker>();
                _ = worker.StartAsync(CancellationToken.None);

                var loginWindow = _serviceProvider.GetRequiredService<Views.LoginWindow>();
                if (loginWindow.ShowDialog() != true)
                {
                    Shutdown();
                    return;
                }

                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                File.WriteAllText("startup_error.txt", ex.ToString());
                MessageBox.Show($"Error al iniciar la aplicación: {ex.Message}\n\nRevisa startup_error.txt para más detalles.");
                Shutdown();
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            File.AppendAllText("runtime_error.txt", e.Exception.ToString());
            MessageBox.Show($"Ocurrió un error inesperado: {e.Exception.Message}");
            e.Handled = true;
        }
    }
}
