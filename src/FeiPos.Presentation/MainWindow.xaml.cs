using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using FeiPos.Presentation.ViewModels;
using FeiPos.Presentation.Views;

namespace FeiPos.Presentation
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            
            this.Loaded += (s, e) => {
                // Arrancar directo en caja; el menú queda oculto hasta pedirlo.
                if (NavView.MenuItems.Count > 1)
                {
                    NavView.SelectedItem = NavView.MenuItems[1];
                    NavigateToTag("Sales");
                }
            };
        }

        public void ShowNavigationPane()
        {
            NavView.IsPaneOpen = true;
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

        private void NavigateToTag(string tag)
        {
            object? view = tag switch
            {
                "Dashboard" => new DashboardView { DataContext = _serviceProvider.GetRequiredService<DashboardViewModel>() },
                "Sales" => new SalesView { DataContext = _serviceProvider.GetRequiredService<SalesViewModel>() },
                "Settings" => new SettingsView { DataContext = _serviceProvider.GetRequiredService<SettingsViewModel>() },
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
    }
}
