using System;
using System.Collections.Generic;

namespace FeiPos.Domain.Entities
{
    public class Sale
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal Total => SubTotal + TotalTax - TotalDiscount;
        
        public SaleStatus Status { get; set; } = SaleStatus.Draft;
        
        // Relaciones
        public List<SaleItem> Items { get; set; } = new();
        public Customer? Customer { get; set; }
        public Guid? CustomerId { get; set; }
        
        // Datos para Hacienda
        public string? HaciendaKey { get; set; } // Clave de 50 dígitos
        public string? ConsecutiveNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerTaxId { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "Cash";
        public ElectronicInvoiceStatus InvoiceStatus { get; set; } = ElectronicInvoiceStatus.None;
    }

    public class SaleItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; } // IVA (e.g. 13)
        public decimal TaxAmount => (UnitPrice * Quantity) * (TaxRate / 100);
        public decimal Total => (UnitPrice * Quantity) + TaxAmount;
    }

    public enum SaleStatus
    {
        Draft,
        Finalized,
        Cancelled
    }

    public enum ElectronicInvoiceStatus
    {
        None,
        PendingSend,
        Sent,
        Accepted,
        Rejected,
        Error
    }
}
