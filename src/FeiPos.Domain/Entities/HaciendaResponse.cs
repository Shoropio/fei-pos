using System;

namespace FeiPos.Domain.Entities
{
    public class HaciendaResponse
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SaleId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected, Error
        public string XmlResponse { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}
