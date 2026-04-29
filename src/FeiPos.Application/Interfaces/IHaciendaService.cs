using System;
using System.Threading.Tasks;
using FeiPos.Domain.Entities;

namespace FeiPos.Application.Interfaces
{
    public interface IHaciendaService
    {
        Task<string> GenerateXml(Sale sale);
        Task<string> SignXml(string xmlRaw, string certificatePath, string pin);
        Task<bool> SendToHacienda(string signedXml, string token);
        Task<ElectronicInvoiceStatus> CheckStatus(string key);
    }

    public interface IFiscalRegistry
    {
        string GenerateConsecutive(string terminal, string office, string docType);
        string GenerateKey(string countryCode, string day, string month, string year, string taxId, string consecutive, string situation, string securityCode);
    }
}
