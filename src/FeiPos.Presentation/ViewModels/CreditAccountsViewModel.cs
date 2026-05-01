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
    public partial class CreditAccountsViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;

        [ObservableProperty] private ObservableCollection<CreditAccountRow> _accounts = new();
        [ObservableProperty] private ObservableCollection<CustomerCreditPayment> _payments = new();
        [ObservableProperty] private CreditAccountRow? _selectedAccount;
        [ObservableProperty] private decimal _paymentAmount;
        [ObservableProperty] private string _paymentReference = string.Empty;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public CreditAccountsViewModel(AppDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
            LoadAccounts();
        }

        partial void OnSelectedAccountChanged(CreditAccountRow? value)
        {
            Payments = value == null
                ? new ObservableCollection<CustomerCreditPayment>()
                : new ObservableCollection<CustomerCreditPayment>(
                    _context.CustomerCreditPayments
                        .Where(p => p.CustomerId == value.CustomerId)
                        .OrderByDescending(p => p.CreatedAt)
                        .ToList());
        }

        [RelayCommand]
        private void LoadAccounts()
        {
            var creditSales = _context.Sales
                .Where(s => s.Status == SaleStatus.Finalized && s.PaymentMethod == "Credit" && s.CustomerId != null)
                .Select(s => new
                {
                    CustomerId = s.CustomerId!.Value,
                    CustomerName = s.CustomerName,
                    s.SubTotal,
                    s.TotalTax,
                    s.TotalDiscount
                })
                .AsEnumerable()
                .GroupBy(s => new { s.CustomerId, s.CustomerName })
                .Select(g => new
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = string.IsNullOrWhiteSpace(g.Key.CustomerName) ? "Cliente" : g.Key.CustomerName,
                    CreditTotal = g.Sum(s => s.SubTotal + s.TotalTax - s.TotalDiscount)
                })
                .ToList();
            var payments = _context.CustomerCreditPayments
                .Select(p => new { p.CustomerId, p.Amount })
                .AsEnumerable()
                .GroupBy(p => p.CustomerId)
                .Select(g => new { CustomerId = g.Key, PaidTotal = g.Sum(p => p.Amount) })
                .ToList();

            var rows = creditSales
                .Select(s =>
                {
                    var paid = payments.FirstOrDefault(p => p.CustomerId == s.CustomerId)?.PaidTotal ?? 0;
                    return new CreditAccountRow
                    {
                        CustomerId = s.CustomerId,
                        CustomerName = s.CustomerName ?? "Cliente",
                        CreditTotal = s.CreditTotal,
                        PaidTotal = paid,
                        Balance = s.CreditTotal - paid
                    };
                })
                .Where(r => r.Balance != 0 || r.CreditTotal > 0)
                .OrderByDescending(r => r.Balance)
                .ToList();

            Accounts = new ObservableCollection<CreditAccountRow>(rows);
            StatusMessage = $"{rows.Count} cuentas de crédito";
        }

        [RelayCommand]
        private void AddPayment()
        {
            if (SelectedAccount == null || PaymentAmount <= 0) return;
            _context.CustomerCreditPayments.Add(new CustomerCreditPayment
            {
                CustomerId = SelectedAccount.CustomerId,
                Amount = PaymentAmount,
                Reference = string.IsNullOrWhiteSpace(PaymentReference) ? "Abono" : PaymentReference.Trim(),
                UserName = _authService.CurrentUserName
            });
            _context.SaveChanges();
            PaymentAmount = 0;
            PaymentReference = string.Empty;
            LoadAccounts();
            StatusMessage = "Abono registrado";
        }
    }

    public class CreditAccountRow
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal CreditTotal { get; set; }
        public decimal PaidTotal { get; set; }
        public decimal Balance { get; set; }
    }
}
