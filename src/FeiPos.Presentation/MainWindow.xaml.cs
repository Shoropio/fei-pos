using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using FeiPos.Presentation.ViewModels;
using FeiPos.Presentation.Views;
using FeiPos.Infrastructure.Services;

namespace FeiPos.Presentation
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AuthService _authService;
        private bool _isFullScreen;

        public MainWindow(IServiceProvider serviceProvider, AuthService authService)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _authService = authService;
            SessionText.Text = $"{_authService.CurrentUserName} ({_authService.CurrentUser?.Role})";
            
            this.Loaded += (s, e) => {
                // Arrancar directo en caja; el menú queda oculto hasta pedirlo.
                if (NavView.MenuItems.Count > 1)
                {
                    NavView.SelectedItem = NavView.MenuItems[1];
                    NavigateToTag("Sales");
                }

                EnterFullScreen();
            };
        }

        public void ShowNavigationPane()
        {
            NavView.IsPaneOpen = true;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isFullScreen)
            {
                ExitFullScreen();
                return;
            }

            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
                return;
            }

            if (!_isFullScreen && e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && _isFullScreen)
            {
                ExitFullScreen();
                e.Handled = true;
            }
        }

        private void ToggleFullScreen()
        {
            if (_isFullScreen)
            {
                ExitFullScreen();
            }
            else
            {
                EnterFullScreen();
            }
        }

        private void EnterFullScreen()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _isFullScreen = true;
        }

        private void ExitFullScreen()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            WindowState = WindowState.Maximized;
            _isFullScreen = false;
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                NavigateToTag("Settings");
            }
            else if (args.SelectedItemContainer != null)
            {
                var tag = args.SelectedItemContainer.Tag?.ToString();
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    NavigateToTag(tag);
                }
            }
        }

        public void NavigateToTag(string tag)
        {
            if (tag == "Logout")
            {
                _authService.Logout();
                ShowLoginAfterLogout();
                return;
            }

            if (!CanNavigate(tag))
            {
                MessageBox.Show("No tiene permiso para abrir esta pantalla.", "Permiso requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            object? view = tag switch
            {
                "Dashboard" => new DashboardView { DataContext = _serviceProvider.GetRequiredService<DashboardViewModel>() },
                "Sales" => new SalesView { DataContext = _serviceProvider.GetRequiredService<SalesViewModel>() },
                "SalesHistory" => new SalesHistoryView { DataContext = _serviceProvider.GetRequiredService<SalesHistoryViewModel>() },
                "FiscalInvoices" => new FiscalInvoicesView { DataContext = _serviceProvider.GetRequiredService<FiscalInvoicesViewModel>() },
                "OpenOrders" => new OpenOrdersView { DataContext = _serviceProvider.GetRequiredService<OpenOrdersViewModel>() },
                "CashDrawer" => new CashDrawerView { DataContext = _serviceProvider.GetRequiredService<CashDrawerViewModel>() },
                "DayClose" => new DayCloseView { DataContext = _serviceProvider.GetRequiredService<DayCloseViewModel>() },
                "Users" => new UsersView { DataContext = _serviceProvider.GetRequiredService<UsersViewModel>() },
                "CreditAccounts" => new CreditAccountsView { DataContext = _serviceProvider.GetRequiredService<CreditAccountsViewModel>() },
                "Settings" => new SettingsView { DataContext = _serviceProvider.GetRequiredService<SettingsViewModel>() },
                "Backups" => new BackupsView { DataContext = _serviceProvider.GetRequiredService<BackupsViewModel>() },
                "Reports" => new QueueView { DataContext = _serviceProvider.GetRequiredService<QueueViewModel>() },
                "Inventory" => new InventoryView { DataContext = _serviceProvider.GetRequiredService<InventoryViewModel>() },
                "Customers" => new CustomerView { DataContext = _serviceProvider.GetRequiredService<CustomerViewModel>() },
                _ => null
            };

            if (view != null)
            {
                ContentFrame.Content = view;
                NavView.IsPaneOpen = false;
            }
        }

        private bool CanNavigate(string tag)
        {
            if (_authService.IsAdmin)
            {
                return true;
            }

            return tag is "Sales" or "OpenOrders" or "Customers";
        }

        private void ShowLoginAfterLogout()
        {
            Hide();

            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            if (loginWindow.ShowDialog() == true && _authService.IsAuthenticated)
            {
                SessionText.Text = $"{_authService.CurrentUserName} ({_authService.CurrentUser?.Role})";
                Show();
                NavigateToTag("Sales");
                return;
            }

            Close();
        }
    }
}
