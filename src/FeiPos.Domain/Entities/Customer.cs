using System;

namespace FeiPos.Domain.Entities
{
    public class Customer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        
        // Datos Fiscales Costa Rica
        public string Identification { get; set; } = string.Empty; // Cédula Física/Jurídica/DIMEX
        public IdentificationType IdType { get; set; }
        
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public enum IdentificationType
    {
        Physical = 1,
        Legal = 2,
        DIMEX = 3,
        NITE = 4
    }
}
