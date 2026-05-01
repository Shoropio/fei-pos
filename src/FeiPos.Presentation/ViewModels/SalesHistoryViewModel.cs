using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeiPos.Domain.Entities;
using FeiPos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FeiPos.Presentation.ViewModels
{
    public partial class SalesHistoryViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty] private ObservableCollection<SaleHistoryRow> _sales = new();
        [ObservableProperty] private ObservableCollection<SaleItem> _items = new();
        [ObservableProperty] private SaleHistoryRow? _selectedSale;
        [ObservableProperty] private DateTime _fromDate = DateTime.Today.AddDays(-7);
        [ObservableProperty] private DateTime _toDate = DateTime.Today.AddDays(1);
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public SalesHistoryViewModel(AppDbContext context)
        {
            _context = context;
            LoadSales();
        }

        partial void OnSelectedSaleChanged(SaleHistoryRow? value)
        {
            Items = value == null
                ? new ObservableCollection<SaleItem>()
                : new ObservableCollection<SaleItem>(_context.Sales.Include(s => s.Items).First(s => s.Id == value.Id).Items);
        }

        [RelayCommand]
        private void LoadSales()
        {
            var text = SearchText.Trim();
            var rows = _context.Sales
                .Include(s => s.Items)
                .Where(s => s.Status != SaleStatus.Draft)
                .Where(s => s.CreatedAt >= FromDate && s.CreatedAt < ToDate)
                .Where(s => string.IsNullOrWhiteSpace(text) ||
                            (s.CustomerName != null && s.CustomerName.Contains(text)) ||
                            (s.ConsecutiveNumber != null && s.ConsecutiveNumber.Contains(text)) ||
                            s.PaymentMethod.Contains(text))
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new SaleHistoryRow
                {
                    Id = s.Id,
                    CreatedAt = s.CreatedAt,
                    CustomerName = string.IsNullOrWhiteSpace(s.CustomerName) ? "Cliente de Contado" : s.CustomerName,
                    PaymentMethod = s.PaymentMethod,
                    Status = s.Status.ToString(),
                    ItemCount = s.Items.Count,
                    Total = s.Total
                })
                .ToList();

            Sales = new ObservableCollection<SaleHistoryRow>(rows);
            StatusMessage = $"{rows.Count} ventas encontradas";
        }

        [RelayCommand]
        private void VoidSelected()
        {
            if (SelectedSale == null) return;
            var sale = _context.Sales
                .Include(s => s.Items)
                .FirstOrDefault(s => s.Id == SelectedSale.Id);
            if (sale == null) return;

            if (sale.Status == SaleStatus.Finalized)
            {
                foreach (var item in sale.Items)
                {
                    var product = _context.Products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null && !product.IsService)
                    {
                        product.Stock += item.Quantity;
                    }
                }
            }

            sale.Status = SaleStatus.Cancelled;
            _context.SaveChanges();
            LoadSales();
            StatusMessage = "Venta anulada e inventario restaurado";
        }
    }

    public class SaleHistoryRow
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal Total { get; set; }
    }
}
