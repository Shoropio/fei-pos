using System;
using System.Threading.Tasks;
using FeiPos.Domain.Entities;
using FeiPos.Application.Interfaces;

namespace FeiPos.Application.Services
{
    public class SalesService
    {
        private readonly IHaciendaService _haciendaService;

        public SalesService(IHaciendaService haciendaService)
        {
            _haciendaService = haciendaService;
        }

        public async Task<Sale> ProcessNewSale(Sale sale)
        {
            // 1. Generar Consecutivo y Clave
            sale.ConsecutiveNumber = "00100001010000000001"; // Ejemplo
            sale.HaciendaKey = "50628042600310112345600100001010000000001198765432";
            sale.Status = SaleStatus.Finalized;
            sale.InvoiceStatus = ElectronicInvoiceStatus.PendingSend;

            // 2. Persistir en Base de Datos (a través de UoW o DbContext)
            // ...

            // 3. Generar XML
            var xml = await _haciendaService.GenerateXml(sale);
            
            // 4. Firmar XML (Asíncrono para no bloquear la UI)
            _ = Task.Run(async () => {
                try {
                    var signedXml = await _haciendaService.SignXml(xml, "cert.p12", "pin123");
                    await _haciendaService.SendToHacienda(signedXml, "token");
                } catch {
                    // Log error and keep in queue
                }
            });

            return sale;
        }
    }
}
