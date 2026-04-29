using System;
using System.Linq;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FeiPos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FeiPos.Presentation.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty]
        private decimal _dailySales;

        [ObservableProperty]
        private decimal _dailyTax;

        [ObservableProperty]
        private int _totalTransactions;

        [ObservableProperty]
        private ObservableCollection<ProductMetric> _topProducts = new();

        public DashboardViewModel(AppDbContext context)
        {
            _context = context;
            LoadMetrics();
        }

        private void LoadMetrics()
        {
            var today = DateTime.Today;
            var todaySales = _context.Sales
                .Include(s => s.Items)
                .Where(s => s.CreatedAt >= today)
                .ToList();

            DailySales = todaySales.Sum(s => s.Total);
            DailyTax = todaySales.Sum(s => s.TotalTax);
            TotalTransactions = todaySales.Count;

            // Productos más vendidos
            var topList = todaySales.SelectMany(s => s.Items)
                .GroupBy(i => i.ProductName)
                .Select(g => new ProductMetric 
                { 
                    Name = g.Key, 
                    Quantity = g.Sum(i => i.Quantity),
                    Total = g.Sum(i => i.Total)
                })
                .OrderByDescending(m => m.Quantity)
                .Take(5)
                .ToList();

            TopProducts = new ObservableCollection<ProductMetric>(topList);
        }
    }

    public class ProductMetric
    {
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Total { get; set; }
    }
}
