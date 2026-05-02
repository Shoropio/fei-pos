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
    public partial class CashDrawerViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;
        private readonly EscPosPrinterService _printerService;

        [ObservableProperty] private ObservableCollection<CashDrawerEntry> _entries = new();
        [ObservableProperty] private CashDrawerEntryType _selectedType = CashDrawerEntryType.Deposit;
        [ObservableProperty] private decimal _amount;
        [ObservableProperty] private string _reason = string.Empty;
        [ObservableProperty] private decimal _depositsTotal;
        [ObservableProperty] private decimal _withdrawalsTotal;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public Array EntryTypes => Enum.GetValues(typeof(CashDrawerEntryType));

        public CashDrawerViewModel(AppDbContext context, AuthService authService, EscPosPrinterService printerService)
        {
            _context = context;
            _authService = authService;
            _printerService = printerService;
            LoadEntries();
        }

        [RelayCommand]
        private void LoadEntries()
        {
            var today = DateTime.Today;
            var rows = _context.CashDrawerEntries
                .Where(e => e.CreatedAt >= today)
                .OrderByDescending(e => e.CreatedAt)
                .ToList();

            Entries = new ObservableCollection<CashDrawerEntry>(rows);
            DepositsTotal = rows.Where(e => e.Type == CashDrawerEntryType.Deposit).Sum(e => e.Amount);
            WithdrawalsTotal = rows.Where(e => e.Type == CashDrawerEntryType.Withdrawal).Sum(e => e.Amount);
            StatusMessage = $"{rows.Count} movimientos de caja hoy";
        }

        [RelayCommand]
        private void AddEntry()
        {
            if (Amount <= 0) return;

            var entry = new CashDrawerEntry
            {
                Type = SelectedType,
                Amount = Amount,
                Reason = string.IsNullOrWhiteSpace(Reason) ? "Movimiento de caja" : Reason.Trim(),
                UserName = _authService.CurrentUserName
            };

            _context.CashDrawerEntries.Add(entry);
            _context.SaveChanges();

            try { _printerService.PrintCashEntry(entry); } catch { }
            if (entry.Type == CashDrawerEntryType.Withdrawal)
            {
                try { _printerService.OpenDrawer(); } catch { }
            }

            Amount = 0;
            Reason = string.Empty;
            LoadEntries();
        }
    }
}
