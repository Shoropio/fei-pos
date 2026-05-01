using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Linq;
using FeiPos.Domain.Entities;
using ModernWpf.Controls;
using FeiPos.Presentation.ViewModels;

namespace FeiPos.Presentation.Views
{
    public partial class SalesView : UserControl
    {
        public SalesView()
        {
            InitializeComponent();
            DataContextChanged += SalesView_DataContextChanged;
        }

        private void SalesView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SalesViewModel oldViewModel)
            {
                oldViewModel.SaleCompleted -= OnSaleCompleted;
                oldViewModel.CheckoutBlocked -= OnCheckoutBlocked;
            }

            if (e.NewValue is SalesViewModel newViewModel)
            {
                newViewModel.SaleCompleted += OnSaleCompleted;
                newViewModel.CheckoutBlocked += OnCheckoutBlocked;
            }
        }

        private async void OnSaleCompleted(string message)
        {
            HideSearchSuggestions();

            var dialog = new ContentDialog
            {
                Title = "Venta completada",
                Content = message,
                PrimaryButtonText = "Aceptar",
                DefaultButton = ContentDialogButton.Primary
            };

            await dialog.ShowAsync();
            SearchBox.Focus();
        }

        private async void OnCheckoutBlocked(string message)
        {
            HideSearchSuggestions();

            var dialog = new ContentDialog
            {
                Title = "No se pudo cobrar",
                Content = message,
                PrimaryButtonText = "Aceptar",
                DefaultButton = ContentDialogButton.Primary
            };

            await dialog.ShowAsync();
            SearchBox.Focus();
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (DataContext is SalesViewModel viewModel)
            {
                if (args.ChosenSuggestion is Product product)
                {
                    viewModel.AddToCartCommand.Execute(product);
                    ResetSearchBox(sender);
                    return;
                }

                var query = args.QueryText;
                if (!string.IsNullOrEmpty(query))
                {
                    viewModel.SubmitSearchCommand.Execute(query);
                    ResetSearchBox(sender);
                }
            }
        }

        private void ResetSearchBox(AutoSuggestBox sender)
        {
            sender.Text = string.Empty;
            HideSearchSuggestions();
            sender.Focus();
        }

        private void HideSearchSuggestions()
        {
            SearchBox.IsSuggestionListOpen = false;
        }

        private async void DiscountButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not SalesViewModel viewModel) return;

            var amountBox = new TextBox
            {
                Text = viewModel.Discount > 0 ? viewModel.Discount.ToString("N2") : string.Empty,
                MinWidth = 260
            };

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = $"Total actual: ₡{viewModel.Total:N2}", Margin = new System.Windows.Thickness(0, 0, 0, 8) });
            panel.Children.Add(amountBox);

            var dialog = new ContentDialog
            {
                Title = "Descuento",
                Content = panel,
                PrimaryButtonText = "Aplicar",
                SecondaryButtonText = "5%",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Secondary)
            {
                viewModel.ApplyDiscountCommand.Execute(null);
            }
            else if (result == ContentDialogResult.Primary &&
                     decimal.TryParse(amountBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount))
            {
                viewModel.ApplyDiscountCommand.Execute(amount);
            }

            SearchBox.Focus();
        }

        private async void CommentButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not SalesViewModel viewModel) return;

            var commentBox = new TextBox
            {
                MinWidth = 320,
                MinHeight = 90,
                AcceptsReturn = true,
                TextWrapping = System.Windows.TextWrapping.Wrap
            };

            var dialog = new ContentDialog
            {
                Title = "Comentario de orden",
                Content = commentBox,
                PrimaryButtonText = "Agregar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                viewModel.AddCommentCommand.Execute(commentBox.Text);
            }

            SearchBox.Focus();
        }

        private async void CustomerButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not SalesViewModel viewModel) return;

            var selector = new ComboBox
            {
                ItemsSource = viewModel.Customers,
                DisplayMemberPath = "FullName",
                SelectedItem = viewModel.SelectedCustomer,
                MinWidth = 320
            };

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "Cliente activo para factura", Margin = new System.Windows.Thickness(0, 0, 0, 8) });
            panel.Children.Add(selector);

            var dialog = new ContentDialog
            {
                Title = "Cliente",
                Content = panel,
                PrimaryButtonText = "Seleccionar",
                SecondaryButtonText = "Contado",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                viewModel.SelectedCustomer = selector.SelectedItem as Customer;
                viewModel.StatusMessage = viewModel.SelectedCustomer == null
                    ? "Cliente de contado"
                    : $"Cliente: {viewModel.SelectedCustomer.FullName}";
            }
            else if (result == ContentDialogResult.Secondary)
            {
                viewModel.ClearCustomerCommand.Execute(null);
            }

            SearchBox.Focus();
        }

        private async void PaymentButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not SalesViewModel viewModel) return;

            var paymentType = new ComboBox
            {
                ItemsSource = new[]
                {
                    new PaymentOption("Efectivo", "Cash"),
                    new PaymentOption("Tarjeta", "Card"),
                    new PaymentOption("Cheque", "Check"),
                    new PaymentOption("Credito", "Credit")
                },
                SelectedIndex = 0,
                MinWidth = 220
            };
            var tenderedBox = new TextBox
            {
                Text = viewModel.Total.ToString("N2"),
                MinWidth = 220
            };
            var changeText = new TextBlock { Text = "Cambio: ₡0.00", FontWeight = System.Windows.FontWeights.SemiBold };

            tenderedBox.TextChanged += (_, _) =>
            {
                if (decimal.TryParse(tenderedBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var tendered))
                {
                    changeText.Text = $"Cambio: ₡{Math.Max(0, tendered - viewModel.Total):N2}";
                }
            };

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = $"Total a cobrar: ₡{viewModel.Total:N2}", FontWeight = System.Windows.FontWeights.Bold, Margin = new System.Windows.Thickness(0, 0, 0, 8) });
            panel.Children.Add(paymentType);
            panel.Children.Add(new TextBlock { Text = "Recibido", Margin = new System.Windows.Thickness(0, 12, 0, 4) });
            panel.Children.Add(tenderedBox);
            panel.Children.Add(changeText);

            var dialog = new ContentDialog
            {
                Title = "Pago",
                Content = panel,
                PrimaryButtonText = "Cobrar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var selectedPayment = paymentType.SelectedItem as PaymentOption;
                viewModel.CheckoutCommand.Execute(selectedPayment?.Value ?? "Pago avanzado");
            }

            SearchBox.Focus();
        }

        private async void OpenOrdersButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not SalesViewModel viewModel) return;

            viewModel.ShowOperationalMenuCommand.Execute(null);

            var list = new ListBox
            {
                ItemsSource = viewModel.OpenOrders,
                DisplayMemberPath = "CustomerName",
                MinWidth = 340,
                MinHeight = 160
            };

            var dialog = new ContentDialog
            {
                Title = "Ventas abiertas",
                Content = list,
                PrimaryButtonText = "Cargar",
                CloseButtonText = "Cerrar",
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary && list.SelectedItem is OpenOrderSummary order)
            {
                viewModel.LoadOrderCommand.Execute(order.Id);
            }

            SearchBox.Focus();
        }

        private async void OperationalMenuButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not SalesViewModel viewModel) return;

            viewModel.ShowOperationalMenuCommand.Execute(null);

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = $"Ventas abiertas: {viewModel.OpenOrders.Count}", Margin = new System.Windows.Thickness(0, 0, 0, 6) });
            panel.Children.Add(new TextBlock { Text = $"Caja actual: ₡{viewModel.Total:N2}" });
            panel.Children.Add(new TextBlock { Text = "Historial, depositos/retiros y cierre de dia quedan listos para conectar a reportes persistentes.", TextWrapping = System.Windows.TextWrapping.Wrap, Margin = new System.Windows.Thickness(0, 10, 0, 0) });

            var dialog = new ContentDialog
            {
                Title = "Menu operacional",
                Content = panel,
                PrimaryButtonText = "Nueva venta",
                SecondaryButtonText = "Guardar orden",
                CloseButtonText = "Cerrar"
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                viewModel.NewSaleCommand.Execute(null);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                viewModel.SaveOrderCommand.Execute(null);
            }

            SearchBox.Focus();
        }

        private void ShellMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.ShowNavigationPane();
            }
        }

        private sealed record PaymentOption(string Label, string Value)
        {
            public override string ToString() => Label;
        }
    }
}
