using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeiPos.Domain.Entities;
using FeiPos.Infrastructure.Persistence;
using FeiPos.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FeiPos.Presentation.ViewModels
{
    public partial class FiscalInvoicesViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly EscPosPrinterService _printerService;
        private readonly AuthService _authService;

        [ObservableProperty] private ObservableCollection<FiscalInvoiceRow> _invoices = new();
        [ObservableProperty] private ObservableCollection<SaleItem> _items = new();
        [ObservableProperty] private ObservableCollection<HaciendaResponse> _fiscalEvents = new();
        [ObservableProperty] private FiscalInvoiceRow? _selectedInvoice;
        [ObservableProperty] private DateTime _fromDate = DateTime.Today.AddDays(-7);
        [ObservableProperty] private DateTime _toDate = DateTime.Today.AddDays(1);
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private string _selectedInvoiceStatus = "Todos";
        [ObservableProperty] private string _selectedFiscalStatus = "Todos";
        [ObservableProperty] private string _xmlRequest = string.Empty;
        [ObservableProperty] private string _signedXml = string.Empty;
        [ObservableProperty] private string _xmlResponse = string.Empty;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public string[] InvoiceStatuses { get; } = { "Todos", "Finalized", "Cancelled" };
        public string[] FiscalStatuses { get; } = { "Todos", "None", "PendingSend", "Sent", "Accepted", "Rejected", "Error" };
        public bool CanCancelInvoice => _authService.IsAdmin;

        public FiscalInvoicesViewModel(AppDbContext context, EscPosPrinterService printerService, AuthService authService)
        {
            _context = context;
            _printerService = printerService;
            _authService = authService;
            LoadInvoices();
        }

        partial void OnSelectedInvoiceChanged(FiscalInvoiceRow? value)
        {
            if (value == null)
            {
                Items = new ObservableCollection<SaleItem>();
                FiscalEvents = new ObservableCollection<HaciendaResponse>();
                XmlRequest = string.Empty;
                SignedXml = string.Empty;
                XmlResponse = string.Empty;
                return;
            }

            var sale = _context.Sales.Include(s => s.Items).FirstOrDefault(s => s.Id == value.Id);
            Items = sale == null
                ? new ObservableCollection<SaleItem>()
                : new ObservableCollection<SaleItem>(sale.Items);

            var events = _context.HaciendaResponses
                .Where(r => r.SaleId == value.Id)
                .OrderByDescending(r => r.ReceivedAt)
                .ToList();

            FiscalEvents = new ObservableCollection<HaciendaResponse>(events);
            var latest = events.FirstOrDefault();
            XmlRequest = latest?.XmlRequest ?? string.Empty;
            SignedXml = latest?.SignedXml ?? string.Empty;
            XmlResponse = latest?.XmlResponse ?? string.Empty;
        }

        [RelayCommand]
        private void LoadInvoices()
        {
            var text = SearchText.Trim();
            var query = _context.Sales
                .Where(s => s.Status != SaleStatus.Draft)
                .Where(s => s.CreatedAt >= FromDate.Date && s.CreatedAt < ToDate.Date.AddDays(1));

            if (SelectedInvoiceStatus != "Todos" && Enum.TryParse<SaleStatus>(SelectedInvoiceStatus, out var saleStatus))
            {
                query = query.Where(s => s.Status == saleStatus);
            }

            if (SelectedFiscalStatus != "Todos" && Enum.TryParse<ElectronicInvoiceStatus>(SelectedFiscalStatus, out var fiscalStatus))
            {
                query = query.Where(s => s.InvoiceStatus == fiscalStatus);
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                query = query.Where(s =>
                    (s.CustomerName != null && s.CustomerName.Contains(text)) ||
                    (s.CustomerTaxId != null && s.CustomerTaxId.Contains(text)) ||
                    (s.ConsecutiveNumber != null && s.ConsecutiveNumber.Contains(text)) ||
                    (s.HaciendaKey != null && s.HaciendaKey.Contains(text)) ||
                    s.PaymentMethod.Contains(text));
            }

            var sales = query
                .Include(s => s.Items)
                .OrderByDescending(s => s.CreatedAt)
                .Take(300)
                .ToList();

            var saleIds = sales.Select(s => s.Id).ToHashSet();
            var latestEvents = _context.HaciendaResponses
                .Where(r => saleIds.Contains(r.SaleId))
                .AsEnumerable()
                .GroupBy(r => r.SaleId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.ReceivedAt).FirstOrDefault());

            var rows = sales.Select(s =>
            {
                latestEvents.TryGetValue(s.Id, out var fiscalEvent);
                return new FiscalInvoiceRow
                {
                    Id = s.Id,
                    CreatedAt = s.CreatedAt,
                    CustomerName = string.IsNullOrWhiteSpace(s.CustomerName) ? "Cliente de Contado" : s.CustomerName,
                    CustomerTaxId = s.CustomerTaxId ?? string.Empty,
                    ConsecutiveNumber = s.ConsecutiveNumber ?? string.Empty,
                    HaciendaKey = s.HaciendaKey ?? string.Empty,
                    PaymentMethod = s.PaymentMethod,
                    SaleStatus = s.Status.ToString(),
                    InvoiceStatus = s.InvoiceStatus.ToString(),
                    FiscalEventStatus = fiscalEvent?.Status ?? "Sin registro",
                    ItemCount = s.Items.Count,
                    Total = s.Total
                };
            }).ToList();

            Invoices = new ObservableCollection<FiscalInvoiceRow>(rows);
            StatusMessage = $"{rows.Count} facturas encontradas";
        }

        [RelayCommand]
        private void ReprintSelected()
        {
            if (SelectedInvoice == null) return;

            var sale = _context.Sales
                .Include(s => s.Items)
                .FirstOrDefault(s => s.Id == SelectedInvoice.Id);

            if (sale == null)
            {
                StatusMessage = "No se encontro la factura.";
                return;
            }

            _printerService.PrintReceipt(sale);
            StatusMessage = "Recibo enviado a impresora";
        }

        [RelayCommand]
        private void CancelSelected()
        {
            if (SelectedInvoice == null) return;

            if (!_authService.IsAdmin)
            {
                StatusMessage = "Permiso de administrador requerido.";
                return;
            }

            var sale = _context.Sales
                .Include(s => s.Items)
                .FirstOrDefault(s => s.Id == SelectedInvoice.Id);

            if (sale == null)
            {
                StatusMessage = "No se encontro la factura.";
                return;
            }

            if (sale.Status == SaleStatus.Cancelled)
            {
                StatusMessage = "La factura ya esta anulada.";
                return;
            }

            foreach (var item in sale.Items)
            {
                var product = _context.Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null && !product.IsService)
                {
                    product.Stock += item.Quantity;
                }
            }

            sale.Status = SaleStatus.Cancelled;
            _context.HaciendaResponses.Add(new HaciendaResponse
            {
                SaleId = sale.Id,
                Key = sale.HaciendaKey ?? string.Empty,
                Status = "CreditNotePending",
                Message = $"Factura anulada por {_authService.CurrentUserName}. Nota de credito fiscal pendiente.",
                ReceivedAt = DateTime.UtcNow
            });

            _context.SaveChanges();
            LoadInvoices();
            StatusMessage = "Factura anulada. Nota de credito fiscal pendiente de emision.";
        }

        [RelayCommand]
        private void ResendSelected()
        {
            if (SelectedInvoice == null) return;

            var sale = _context.Sales.FirstOrDefault(s => s.Id == SelectedInvoice.Id);
            if (sale == null) return;

            if (sale.InvoiceStatus == ElectronicInvoiceStatus.Accepted)
            {
                StatusMessage = "La factura ya fue aceptada por Hacienda.";
                return;
            }

            sale.InvoiceStatus = ElectronicInvoiceStatus.PendingSend;
            _context.HaciendaResponses.Add(new HaciendaResponse
            {
                SaleId = sale.Id,
                Key = sale.HaciendaKey ?? string.Empty,
                Status = "Requeued",
                Message = $"Re-encolado manual por {_authService.CurrentUserName}.",
                ReceivedAt = DateTime.UtcNow
            });

            _context.SaveChanges();
            LoadInvoices();
            StatusMessage = "Factura re-encolada para envío.";
        }

        [RelayCommand]
        private void PrintCopySelected()
        {
            if (SelectedInvoice == null) return;

            var sale = _context.Sales
                .Include(s => s.Items)
                .FirstOrDefault(s => s.Id == SelectedInvoice.Id);

            if (sale == null) return;

            _printerService.PrintReceiptCopy(sale);
            StatusMessage = "Copia de recibo enviada a impresora";
        }
    }

    public class FiscalInvoiceRow
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerTaxId { get; set; } = string.Empty;
        public string ConsecutiveNumber { get; set; } = string.Empty;
        public string HaciendaKey { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string SaleStatus { get; set; } = string.Empty;
        public string InvoiceStatus { get; set; } = string.Empty;
        public string FiscalEventStatus { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal Total { get; set; }
    }
}
