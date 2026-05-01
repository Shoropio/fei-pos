using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeiPos.Domain.Entities;
using FeiPos.Infrastructure.Persistence;
using FeiPos.Infrastructure.Services;

namespace FeiPos.Presentation.ViewModels
{
    public partial class DayCloseViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;
        private readonly EscPosPrinterService _printerService;

        [ObservableProperty] private DateTime _businessDate = DateTime.Today;
        [ObservableProperty] private decimal _salesTotal;
        [ObservableProperty] private decimal _cashTotal;
        [ObservableProperty] private decimal _cardTotal;
        [ObservableProperty] private decimal _checkTotal;
        [ObservableProperty] private decimal _creditTotal;
        [ObservableProperty] private decimal _depositsTotal;
        [ObservableProperty] private decimal _withdrawalsTotal;
        [ObservableProperty] private decimal _expectedCash;
        [ObservableProperty] private decimal _countedCash;
        [ObservableProperty] private decimal _difference;
        [ObservableProperty] private string _notes = string.Empty;
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private ObservableCollection<DayClosure> _closures = new();

        public DayCloseViewModel(AppDbContext context, AuthService authService, EscPosPrinterService printerService)
        {
            _context = context;
            _authService = authService;
            _printerService = printerService;
            Refresh();
        }

        partial void OnCountedCashChanged(decimal value)
        {
            Difference = CountedCash - ExpectedCash;
        }

        [RelayCommand]
        private void Refresh()
        {
            var start = BusinessDate.Date;
            var end = start.AddDays(1);
            var sales = _context.Sales
                .Where(s => s.Status == SaleStatus.Finalized && s.CreatedAt >= start && s.CreatedAt < end)
                .ToList();
            var movements = _context.CashDrawerEntries
                .Where(e => e.CreatedAt >= start && e.CreatedAt < end)
                .ToList();

            SalesTotal = sales.Sum(s => s.Total);
            CashTotal = sales.Where(s => s.PaymentMethod == "Cash").Sum(s => s.Total);
            CardTotal = sales.Where(s => s.PaymentMethod == "Card").Sum(s => s.Total);
            CheckTotal = sales.Where(s => s.PaymentMethod == "Check").Sum(s => s.Total);
            CreditTotal = sales.Where(s => s.PaymentMethod == "Credit").Sum(s => s.Total);
            DepositsTotal = movements.Where(e => e.Type == CashDrawerEntryType.Deposit).Sum(e => e.Amount);
            WithdrawalsTotal = movements.Where(e => e.Type == CashDrawerEntryType.Withdrawal).Sum(e => e.Amount);
            ExpectedCash = CashTotal + DepositsTotal - WithdrawalsTotal;
            Difference = CountedCash - ExpectedCash;
            Closures = new ObservableCollection<DayClosure>(_context.DayClosures.OrderByDescending(c => c.ClosedAt).Take(20).ToList());
            StatusMessage = "Resumen actualizado";
        }

        [RelayCommand]
        private void CloseDay()
        {
            Refresh();
            var closure = new DayClosure
            {
                BusinessDate = BusinessDate.Date,
                SalesTotal = SalesTotal,
                CashTotal = CashTotal,
                CardTotal = CardTotal,
                CheckTotal = CheckTotal,
                CreditTotal = CreditTotal,
                DepositsTotal = DepositsTotal,
                WithdrawalsTotal = WithdrawalsTotal,
                ExpectedCash = ExpectedCash,
                CountedCash = CountedCash,
                Difference = Difference,
                ClosedBy = _authService.CurrentUserName,
                Notes = Notes
            };

            _context.DayClosures.Add(closure);
            _context.SaveChanges();
            
            _printerService.PrintDayClosure(closure);
            
            Notes = string.Empty;
            Refresh();
            StatusMessage = "Día finalizado y reporte impreso";
        }
    }
}
