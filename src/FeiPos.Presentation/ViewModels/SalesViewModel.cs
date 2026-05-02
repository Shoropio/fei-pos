using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeiPos.Application.Interfaces;
using FeiPos.Domain.Entities;
using FeiPos.Infrastructure.Persistence;
using FeiPos.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FeiPos.Presentation.ViewModels
{
    public partial class SalesViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly IHaciendaService _haciendaService;
        private readonly ConfigurationService _configService;
        private readonly EscPosPrinterService _printerService;
        private readonly AuthService _authService;

        private Guid? _currentDraftSaleId;

        [ObservableProperty]
        private ObservableCollection<Product> _products = new();

        [ObservableProperty]
        private ObservableCollection<Product> _searchResults = new();

        [ObservableProperty]
        private ObservableCollection<OpenOrderSummary> _openOrders = new();

        [ObservableProperty]
        private ObservableCollection<Customer> _customers = new();

        [ObservableProperty]
        private Customer? _selectedCustomer;

        [ObservableProperty]
        private ObservableCollection<SaleItem> _cart = new();

        [ObservableProperty]
        private SaleItem? _selectedCartItem;

        [ObservableProperty]
        private ProductSearchMode _selectedSearchMode = ProductSearchMode.AllFields;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _orderComment = string.Empty;

        [ObservableProperty]
        private decimal _subtotal;

        [ObservableProperty]
        private decimal _tax;

        [ObservableProperty]
        private decimal _discount;

        [ObservableProperty]
        private decimal _total;

        [ObservableProperty]
        private string _statusMessage = "Venta lista";

        public bool CanRunSensitiveActions => _authService.IsAdmin;

        public ObservableCollection<ProductSearchMode> SearchModes { get; } = new(
            Enum.GetValues<ProductSearchMode>());

        public event Action<string>? SaleCompleted;
        public event Action<string>? CheckoutBlocked;

        public SalesViewModel(
            AppDbContext context,
            IHaciendaService haciendaService,
            ConfigurationService configService,
            EscPosPrinterService printerService,
            AuthService authService)
        {
            _context = context;
            _haciendaService = haciendaService;
            _configService = configService;
            _printerService = printerService;
            _authService = authService;

            LoadProducts();
            LoadCustomers();
            LoadOpenOrders();
        }

        partial void OnStatusMessageChanged(string value)
        {
            if (value.StartsWith("Pago ", StringComparison.OrdinalIgnoreCase))
            {
                SaleCompleted?.Invoke("Venta procesada correctamente");
            }
        }

        private void LoadProducts()
        {
            var productList = _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.GroupName)
                .ThenBy(p => p.Name)
                .ToList();

            Products = new ObservableCollection<Product>(productList);
            SearchResults = new ObservableCollection<Product>(productList.Take(18));
        }

        private void LoadCustomers()
        {
            Customers = new ObservableCollection<Customer>(
                _context.Customers.Where(c => c.IsActive).OrderBy(c => c.FullName).ToList());
        }

        private void LoadOpenOrders()
        {
            var drafts = _context.Sales
                .Include(s => s.Items)
                .Where(s => s.Status == SaleStatus.Draft && s.Items.Any())
                .OrderByDescending(s => s.CreatedAt)
                .Take(20)
                .Select(s => new OpenOrderSummary
                {
                    Id = s.Id,
                    CreatedAt = s.CreatedAt,
                    CustomerName = string.IsNullOrWhiteSpace(s.CustomerName) ? "Cliente de Contado" : s.CustomerName,
                    ItemCount = s.Items.Count,
                    Total = s.Total
                })
                .ToList();

            OpenOrders = new ObservableCollection<OpenOrderSummary>(drafts);
        }

        partial void OnSelectedSearchModeChanged(ProductSearchMode value)
        {
            SearchProducts(SearchText);
        }

        partial void OnSearchTextChanged(string value)
        {
            SearchProducts(value);
        }

        [RelayCommand]
        private void SubmitSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                SearchProducts(SearchText);
                return;
            }

            SearchText = query.Trim();
            var matches = FindProducts(SearchText).ToList();
            SearchResults = new ObservableCollection<Product>(matches.Take(18));

            var exact = matches.FirstOrDefault(IsExactSearchMatch) ?? matches.FirstOrDefault();
            if (exact != null)
            {
                AddToCart(exact);
                SearchText = string.Empty;
            }
            else
            {
                StatusMessage = $"Sin resultados para '{query}'.";
            }
        }

        [RelayCommand]
        private void NewSale()
        {
            SaveCurrentOrderAsDraft();
            StartBlankOrder();
            StatusMessage = "Nueva venta abierta";
        }

        [RelayCommand]
        private void SaveOrder()
        {
            SaveCurrentOrderAsDraft();
            StatusMessage = "Orden guardada";
        }

        [RelayCommand]
        private void LoadOrder(Guid orderId)
        {
            SaveCurrentOrderAsDraft();

            var sale = _context.Sales
                .Include(s => s.Items)
                .FirstOrDefault(s => s.Id == orderId && s.Status == SaleStatus.Draft);

            if (sale == null)
            {
                StatusMessage = "La orden ya no está disponible";
                LoadOpenOrders();
                return;
            }

            _currentDraftSaleId = sale.Id;
            Cart = new ObservableCollection<SaleItem>(sale.Items.Select(CloneItem));
            SelectedCustomer = sale.CustomerId.HasValue
                ? Customers.FirstOrDefault(c => c.Id == sale.CustomerId.Value)
                : null;
            OrderComment = sale.Comment;
            Discount = sale.TotalDiscount;
            CalculateTotals();
            StatusMessage = $"Orden {sale.CreatedAt:t} cargada";
        }

        [RelayCommand]
        private void Search()
        {
            SearchProducts(SearchText);
            StatusMessage = "Buscar producto por modo seleccionado";
        }

        [RelayCommand]
        private void SetSearchMode(ProductSearchMode mode)
        {
            SelectedSearchMode = mode;
            StatusMessage = $"Modo de busqueda: {GetSearchModeLabel(mode)}";
        }

        [RelayCommand]
        private void Qty()
        {
            if (SelectedCartItem == null) return;

            SelectedCartItem.Quantity += 1;
            RefreshCart();
            StatusMessage = "Cantidad actualizada";
        }

        [RelayCommand]
        private void RemoveSelectedItem()
        {
            if (!CanRunSensitiveActions)
            {
                StatusMessage = "Permiso de administrador requerido para eliminar lineas.";
                CheckoutBlocked?.Invoke(StatusMessage);
                return;
            }

            if (SelectedCartItem == null)
            {
                if (Cart.Count == 1)
                {
                    SelectedCartItem = Cart.First();
                } else {
                    StatusMessage = "Seleccione una linea para eliminar";
                    CheckoutBlocked?.Invoke(StatusMessage);
                    return;
                }
            }

            var itemToRemove = Cart.FirstOrDefault(i => i.Id == SelectedCartItem.Id)
                ?? Cart.FirstOrDefault(i => i.ProductId == SelectedCartItem.ProductId);

            if (itemToRemove == null)
            {
                StatusMessage = "La linea seleccionada ya no esta en el carrito";
                CheckoutBlocked?.Invoke(StatusMessage);
                return;
            }

            Cart.Remove(itemToRemove);
            SelectedCartItem = null;
            CalculateTotals();
            StatusMessage = "Línea anulada";
        }

        [RelayCommand]
        private void VoidOrder()
        {
            if (!CanRunSensitiveActions)
            {
                StatusMessage = "Permiso de administrador requerido para anular la orden.";
                CheckoutBlocked?.Invoke(StatusMessage);
                return;
            }

            if (_currentDraftSaleId.HasValue)
            {
                var sale = _context.Sales
                    .Include(s => s.Items)
                    .FirstOrDefault(s => s.Id == _currentDraftSaleId.Value);

                if (sale != null)
                {
                    _context.SaleItems.RemoveRange(sale.Items);
                    sale.Status = SaleStatus.Cancelled;
                    _context.SaveChanges();
                }
            }

            StartBlankOrder();
            LoadOpenOrders();
            StatusMessage = "Orden anulada";
        }

        [RelayCommand]
        private void ApplyDiscount(decimal? amount)
        {
            if (!CanRunSensitiveActions)
            {
                StatusMessage = "Permiso de administrador requerido para aplicar descuentos.";
                CheckoutBlocked?.Invoke(StatusMessage);
                return;
            }

            if (!Cart.Any()) return;

            Discount = amount.HasValue && amount.Value >= 0
                ? Math.Min(amount.Value, Subtotal + Tax)
                : Math.Round(Subtotal * 0.05m, 2);
            CalculateTotals();
            StatusMessage = $"Descuento ₡{Discount:N2} aplicado";
        }

        [RelayCommand]
        private void AddComment(string? comment)
        {
            if (string.IsNullOrWhiteSpace(comment)) return;

            OrderComment = string.IsNullOrWhiteSpace(OrderComment)
                ? comment.Trim()
                : $"{OrderComment}; {comment.Trim()}";
            StatusMessage = "Comentario agregado";
        }

        [RelayCommand]
        private void ClearCustomer()
        {
            SelectedCustomer = null;
            StatusMessage = "Cliente de contado";
        }

        [RelayCommand]
        private void SelectCustomer()
        {
            if (!Customers.Any())
            {
                StatusMessage = "No hay clientes activos";
                return;
            }

            if (SelectedCustomer == null)
            {
                SelectedCustomer = Customers.First();
            }
            else
            {
                var index = Customers.IndexOf(SelectedCustomer);
                SelectedCustomer = index >= 0 && index < Customers.Count - 1 ? Customers[index + 1] : null;
            }

            StatusMessage = SelectedCustomer == null
                ? "Cliente de contado"
                : $"Cliente: {SelectedCustomer.FullName}";
        }

        [RelayCommand]
        private void AddToCart(Product product)
        {
            if (product == null) return;

            var existing = Cart.FirstOrDefault(i => i.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity += product.DefaultQuantity <= 0 ? 1 : product.DefaultQuantity;
            } else {
                Cart.Add(new SaleItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = product.DefaultQuantity <= 0 ? 1 : product.DefaultQuantity,
                    TaxRate = product.TaxRate
                });
            }

            RefreshCart();
            SelectedCartItem = Cart.FirstOrDefault(i => i.ProductId == product.Id);
            StatusMessage = $"{product.Name} agregado";
        }

        [RelayCommand]
        private async Task Checkout(string? paymentType)
        {
            if (!Cart.Any()) return;

            var normalizedPaymentType = string.IsNullOrWhiteSpace(paymentType) || paymentType == "Pago avanzado"
                ? "Cash"
                : paymentType;

            if (normalizedPaymentType == "Credit" && SelectedCustomer == null)
            {
                StatusMessage = "Seleccione un cliente para venta a credito";
                CheckoutBlocked?.Invoke(StatusMessage);
                return;
            }

            if (!HasEnoughStockForCheckout())
            {
                return;
            }

            var sale = BuildSaleFromCurrentOrder();
            sale.Status = SaleStatus.Finalized;
            sale.InvoiceStatus = ElectronicInvoiceStatus.PendingSend;
            sale.PaymentMethod = normalizedPaymentType;

            sale.ConsecutiveNumber = FeiPos.Infrastructure.Helpers.FiscalHelper.GenerateConsecutive(
                GetNextFiscalSequence(),
                _configService.Config.OfficeId,
                _configService.Config.TerminalId);
            sale.HaciendaKey = FeiPos.Infrastructure.Helpers.FiscalHelper.GenerateKey(
                _configService.Config.TaxId,
                sale.ConsecutiveNumber);

            PersistFinalSale(sale);
            await _context.SaveChangesAsync();

            if (_configService.Config.OpenDrawerOnSale)
            {
                try { _printerService.OpenDrawer(); } catch { }
            }

            if (_configService.Config.AutoPrintReceipt)
            {
                try { _printerService.PrintReceipt(sale); } catch { }
            }
            _ = SendFiscalDocumentAsync(sale);

            StartBlankOrder();
            LoadProducts();
            LoadOpenOrders();
            StatusMessage = $"Pago {GetPaymentMethodLabel(normalizedPaymentType)} aplicado";
        }

        [RelayCommand]
        private void ShowOperationalMenu()
        {
            LoadOpenOrders();
            StatusMessage = $"Menu: {OpenOrders.Count} ventas abiertas";
        }

        private void SearchProducts(string query)
        {
            var matches = FindProducts(query).Take(18).ToList();
            SearchResults = new ObservableCollection<Product>(matches);
        }

        private IEnumerable<Product> FindProducts(string query)
        {
            var text = (query ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                return Products;
            }

            if (NormalizeSearchText(text).Length < 2)
            {
                return Enumerable.Empty<Product>();
            }

            return SelectedSearchMode switch
            {
                ProductSearchMode.Barcode => Products.Where(p => StartsWithSearchText(p.Barcode, text)),
                ProductSearchMode.Sku => Products.Where(p => StartsWithSearchText(p.Sku, text)),
                ProductSearchMode.Name => Products.Where(p => NameMatchesSearchText(p.Name, text)),
                _ => Products.Where(p =>
                    NameMatchesSearchText(p.Name, text) ||
                    StartsWithSearchText(p.Sku, text) ||
                    StartsWithSearchText(p.Barcode, text))
            };
        }

        private bool IsExactSearchMatch(Product product)
        {
            return SelectedSearchMode switch
            {
                ProductSearchMode.Barcode => EqualsSearchText(product.Barcode, SearchText),
                ProductSearchMode.Sku => EqualsSearchText(product.Sku, SearchText),
                ProductSearchMode.Name => EqualsSearchText(product.Name, SearchText),
                _ => EqualsSearchText(product.Barcode, SearchText) ||
                     EqualsSearchText(product.Sku, SearchText) ||
                     EqualsSearchText(product.Name, SearchText)
            };
        }

        private static bool StartsWithSearchText(string? value, string query)
        {
            return NormalizeSearchText(value).StartsWith(NormalizeSearchText(query), StringComparison.OrdinalIgnoreCase);
        }

        private static bool NameMatchesSearchText(string? value, string query)
        {
            var normalizedValue = NormalizeSearchText(value);
            var normalizedQuery = NormalizeSearchText(query);
            return normalizedQuery.Length >= 2 &&
                   normalizedValue.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase);
        }

        private static bool EqualsSearchText(string? value, string query)
        {
            return string.Equals(NormalizeSearchText(value), NormalizeSearchText(query), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeSearchText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(ch);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string GetSearchModeLabel(ProductSearchMode mode)
        {
            return mode switch
            {
                ProductSearchMode.Barcode => "Codigo de barras",
                ProductSearchMode.Sku => "SKU",
                ProductSearchMode.Name => "Nombre",
                _ => "Todos los campos"
            };
        }

        private static string GetPaymentMethodLabel(string paymentMethod)
        {
            return paymentMethod switch
            {
                "Card" => "tarjeta",
                "Check" => "cheque",
                "Credit" => "credito",
                _ => "efectivo"
            };
        }

        private void SaveCurrentOrderAsDraft()
        {
            if (!Cart.Any())
            {
                return;
            }

            var sale = BuildSaleFromCurrentOrder();
            sale.Status = SaleStatus.Draft;
            sale.InvoiceStatus = ElectronicInvoiceStatus.None;
            PersistDraftSale(sale);
            _context.SaveChanges();
            _currentDraftSaleId = sale.Id;
            LoadOpenOrders();
        }

        private Sale BuildSaleFromCurrentOrder()
        {
            var sale = _currentDraftSaleId.HasValue
                ? _context.Sales.Include(s => s.Items).FirstOrDefault(s => s.Id == _currentDraftSaleId.Value) ?? new Sale()
                : new Sale();

            sale.SubTotal = Subtotal;
            sale.TotalTax = Tax;
            sale.TotalDiscount = Discount;
            sale.Comment = OrderComment;
            sale.PaymentMethod = string.IsNullOrWhiteSpace(sale.PaymentMethod) ? "Cash" : sale.PaymentMethod;
            sale.Customer = SelectedCustomer;
            sale.CustomerId = SelectedCustomer?.Id;
            sale.CustomerName = SelectedCustomer?.FullName;
            sale.CustomerTaxId = SelectedCustomer?.Identification;

            return sale;
        }

        private void PersistDraftSale(Sale sale)
        {
            if (!_context.Sales.Local.Any(s => s.Id == sale.Id) && !_context.Sales.Any(s => s.Id == sale.Id))
            {
                _context.Sales.Add(sale);
            }

            ReplaceSaleItems(sale);
        }

        private void PersistFinalSale(Sale sale)
        {
            if (!_context.Sales.Local.Any(s => s.Id == sale.Id) && !_context.Sales.Any(s => s.Id == sale.Id))
            {
                _context.Sales.Add(sale);
            }

            ReplaceSaleItems(sale);

            foreach (var item in Cart)
            {
                var product = _context.Products.Find(item.ProductId);
                if (product != null && !product.IsService)
                {
                    product.Stock -= item.Quantity;
                }
            }
        }

        private bool HasEnoughStockForCheckout()
        {
            foreach (var group in Cart.GroupBy(i => i.ProductId))
            {
                var product = _context.Products.Find(group.Key);
                if (product == null || product.IsService) continue;

                var requested = group.Sum(i => i.Quantity);
                if (product.Stock < requested)
                {
                    StatusMessage = $"Stock insuficiente para {product.Name}. Disponible: {product.Stock:N2}, solicitado: {requested:N2}";
                    CheckoutBlocked?.Invoke(StatusMessage);
                    return false;
                }
            }

            return true;
        }

        private long GetNextFiscalSequence()
        {
            var prefix = $"{_configService.Config.OfficeId}{_configService.Config.TerminalId}01";
            var maxSequence = _context.Sales
                .Where(s => s.ConsecutiveNumber != null && s.ConsecutiveNumber.StartsWith(prefix))
                .Select(s => s.ConsecutiveNumber!)
                .AsEnumerable()
                .Select(GetSequenceFromConsecutive)
                .DefaultIfEmpty(0)
                .Max();

            return maxSequence + 1;
        }

        private static long GetSequenceFromConsecutive(string consecutive)
        {
            if (consecutive.Length < 10) return 0;
            return long.TryParse(consecutive[^10..], out var sequence) ? sequence : 0;
        }

        private void ReplaceSaleItems(Sale sale)
        {
            if (sale.Items.Any())
            {
                _context.SaleItems.RemoveRange(sale.Items);
                sale.Items.Clear();
            }

            foreach (var item in Cart)
            {
                sale.Items.Add(CloneItem(item));
            }
        }

        private async Task SendFiscalDocumentAsync(Sale sale)
        {
            var response = new HaciendaResponse
            {
                SaleId = sale.Id,
                Key = sale.HaciendaKey ?? string.Empty,
                Status = "Pending",
                Message = "Documento fiscal generado y pendiente de envio.",
                ReceivedAt = DateTime.UtcNow
            };

            try
            {
                var xml = await _haciendaService.GenerateXml(sale);
                var signedXml = await _haciendaService.SignXml(xml, string.Empty, string.Empty);
                response.XmlRequest = xml;
                response.SignedXml = signedXml;
                response.Status = "Signed";
                response.Message = "XML generado y firmado. Envio a Hacienda pendiente.";
                sale.InvoiceStatus = ElectronicInvoiceStatus.PendingSend;
            }
            catch (Exception ex)
            {
                response.Status = "Error";
                response.Message = ex.Message;
                sale.InvoiceStatus = ElectronicInvoiceStatus.Error;
            }
            finally
            {
                _context.HaciendaResponses.Add(response);
                await _context.SaveChangesAsync();
            }
        }

        private void StartBlankOrder()
        {
            _currentDraftSaleId = null;
            Cart.Clear();
            SelectedCartItem = null;
            SelectedCustomer = null;
            OrderComment = string.Empty;
            Discount = 0;
            CalculateTotals();
        }

        private void RefreshCart()
        {
            Cart = new ObservableCollection<SaleItem>(Cart.Select(CloneItem));
            CalculateTotals();
        }

        private void CalculateTotals()
        {
            Subtotal = Cart.Sum(i => i.UnitPrice * i.Quantity);
            Tax = Cart.Sum(i => i.TaxAmount);
            Total = Subtotal + Tax - Discount;
        }

        private static SaleItem CloneItem(SaleItem item)
        {
            return new SaleItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TaxRate = item.TaxRate
            };
        }
    }

    public enum ProductSearchMode
    {
        AllFields,
        Barcode,
        Sku,
        Name
    }

    public class OpenOrderSummary
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal Total { get; set; }
    }
}
