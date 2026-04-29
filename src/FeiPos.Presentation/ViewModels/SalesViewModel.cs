using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        public ObservableCollection<ProductSearchMode> SearchModes { get; } = new(
            Enum.GetValues<ProductSearchMode>());

        public SalesViewModel(
            AppDbContext context,
            IHaciendaService haciendaService,
            ConfigurationService configService,
            EscPosPrinterService printerService)
        {
            _context = context;
            _haciendaService = haciendaService;
            _configService = configService;
            _printerService = printerService;

            LoadProducts();
            LoadCustomers();
            LoadOpenOrders();
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
            StatusMessage = $"Modo de búsqueda: {mode}";
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
            if (SelectedCartItem == null) return;

            Cart.Remove(SelectedCartItem);
            SelectedCartItem = null;
            CalculateTotals();
            StatusMessage = "Línea anulada";
        }

        [RelayCommand]
        private void VoidOrder()
        {
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
            if (!Cart.Any()) return;

            Discount = amount.HasValue && amount.Value >= 0
                ? Math.Min(amount.Value, Subtotal + Tax)
                : Math.Round(Subtotal * 0.05m, 2);
            CalculateTotals();
            StatusMessage = $"Descuento {Discount:N2} aplicado";
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
            }
            else
            {
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
            StatusMessage = $"{product.Name} agregado";
        }

        [RelayCommand]
        private async Task Checkout(string? paymentType)
        {
            if (!Cart.Any()) return;

            var sale = BuildSaleFromCurrentOrder();
            sale.Status = SaleStatus.Finalized;
            sale.InvoiceStatus = ElectronicInvoiceStatus.PendingSend;

            var lastSaleCount = _context.Sales.Count(s => s.Status == SaleStatus.Finalized) + 1;
            sale.ConsecutiveNumber = FeiPos.Infrastructure.Helpers.FiscalHelper.GenerateConsecutive(
                lastSaleCount,
                _configService.Config.OfficeId,
                _configService.Config.TerminalId);
            sale.HaciendaKey = FeiPos.Infrastructure.Helpers.FiscalHelper.GenerateKey(
                _configService.Config.TaxId,
                sale.ConsecutiveNumber);

            PersistFinalSale(sale);
            await _context.SaveChangesAsync();

            _printerService.PrintReceipt(sale);
            _ = SendFiscalDocumentAsync(sale);

            StartBlankOrder();
            LoadProducts();
            LoadOpenOrders();
            StatusMessage = $"Pago {paymentType ?? "rápido"} aplicado";
        }

        [RelayCommand]
        private void ShowOperationalMenu()
        {
            LoadOpenOrders();
            StatusMessage = $"Menú: {OpenOrders.Count} ventas abiertas";
        }

        private void SearchProducts(string query)
        {
            var matches = FindProducts(query).Take(18).ToList();
            SearchResults = new ObservableCollection<Product>(matches);
        }

        private IQueryable<Product> FindProducts(string query)
        {
            var text = (query ?? string.Empty).Trim();
            var source = Products.AsQueryable();

            if (string.IsNullOrWhiteSpace(text))
            {
                return source;
            }

            return SelectedSearchMode switch
            {
                ProductSearchMode.Barcode => source.Where(p => p.Barcode != null && p.Barcode.Contains(text)),
                ProductSearchMode.Sku => source.Where(p => p.Sku.Contains(text)),
                ProductSearchMode.Name => source.Where(p => p.Name.Contains(text)),
                _ => source.Where(p =>
                    p.Name.Contains(text) ||
                    p.Sku.Contains(text) ||
                    (p.Barcode != null && p.Barcode.Contains(text)))
            };
        }

        private bool IsExactSearchMatch(Product product)
        {
            return SelectedSearchMode switch
            {
                ProductSearchMode.Barcode => product.Barcode == SearchText,
                ProductSearchMode.Sku => product.Sku == SearchText,
                ProductSearchMode.Name => product.Name.Equals(SearchText, StringComparison.OrdinalIgnoreCase),
                _ => product.Barcode == SearchText ||
                     product.Sku == SearchText ||
                     product.Name.Equals(SearchText, StringComparison.OrdinalIgnoreCase)
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
            try
            {
                var xml = await _haciendaService.GenerateXml(sale);
                await _haciendaService.SignXml(xml, string.Empty, string.Empty);
            }
            catch
            {
                // La cola fiscal persistente queda como siguiente paso; no bloquea caja.
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
