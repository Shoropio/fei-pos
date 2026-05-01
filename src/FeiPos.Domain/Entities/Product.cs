using System;

namespace FeiPos.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string UnitOfMeasure { get; set; } = "Unid";
        public string GroupName { get; set; } = "General";
        public string? ImagePath { get; set; }
        public string ColorHex { get; set; } = "#455A64";
        public string Comments { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal TaxRate { get; set; } = 13.0m; // Default IVA CR
        public decimal Stock { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsService { get; set; }
        public decimal DefaultQuantity { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public override string ToString()
        {
            return Name;
        }
    }
}
