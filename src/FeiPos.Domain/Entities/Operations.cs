using System;

namespace FeiPos.Domain.Entities
{
    public class CashDrawerEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public CashDrawerEntryType Type { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string UserName { get; set; } = "Admin";
    }

    public enum CashDrawerEntryType
    {
        Deposit = 1,
        Withdrawal = 2
    }

    public class DayClosure
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime BusinessDate { get; set; } = DateTime.Today;
        public DateTime ClosedAt { get; set; } = DateTime.UtcNow;
        public decimal SalesTotal { get; set; }
        public decimal CashTotal { get; set; }
        public decimal CardTotal { get; set; }
        public decimal CheckTotal { get; set; }
        public decimal CreditTotal { get; set; }
        public decimal DepositsTotal { get; set; }
        public decimal WithdrawalsTotal { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal CountedCash { get; set; }
        public decimal Difference { get; set; }
        public string ClosedBy { get; set; } = "Admin";
        public string Notes { get; set; } = string.Empty;
    }

    public class AppUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Cashier";
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsCurrent { get; set; }
        public bool MustChangePassword { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
    }

    public class CustomerCreditPayment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public decimal Amount { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string UserName { get; set; } = "Admin";
    }
}
