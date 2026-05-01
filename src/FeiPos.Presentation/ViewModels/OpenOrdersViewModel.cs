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
    public partial class OpenOrdersViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty] private ObservableCollection<OpenOrderAdminRow> _orders = new();
        [ObservableProperty] private ObservableCollection<SaleItem> _items = new();
        [ObservableProperty] private OpenOrderAdminRow? _selectedOrder;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public OpenOrdersViewModel(AppDbContext context)
        {
            _context = context;
            LoadOrders();
        }

        partial void OnSelectedOrderChanged(OpenOrderAdminRow? value)
        {
            Items = value == null
                ? new ObservableCollection<SaleItem>()
                : new ObservableCollection<SaleItem>(_context.Sales.Include(s => s.Items).First(s => s.Id == value.Id).Items);
        }

        [RelayCommand]
        private void LoadOrders()
        {
            var rows = _context.Sales
                .Include(s => s.Items)
                .Where(s => s.Status == SaleStatus.Draft && s.Items.Any())
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new OpenOrderAdminRow
                {
                    Id = s.Id,
                    CreatedAt = s.CreatedAt,
                    CustomerName = string.IsNullOrWhiteSpace(s.CustomerName) ? "Cliente de Contado" : s.CustomerName,
                    ItemCount = s.Items.Count,
                    Total = s.Total,
                    Comment = s.Comment
                })
                .ToList();

            Orders = new ObservableCollection<OpenOrderAdminRow>(rows);
            StatusMessage = $"{rows.Count} ventas abiertas";
        }

        [RelayCommand]
        private void CancelSelected()
        {
            if (SelectedOrder == null) return;
            var sale = _context.Sales.FirstOrDefault(s => s.Id == SelectedOrder.Id);
            if (sale == null) return;
            sale.Status = SaleStatus.Cancelled;
            _context.SaveChanges();
            LoadOrders();
            Items.Clear();
            StatusMessage = "Venta abierta anulada";
        }
    }

    public class OpenOrderAdminRow
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal Total { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}
