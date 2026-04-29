using System;

namespace FeiPos.Infrastructure.Helpers
{
    public static class FiscalHelper
    {
        public static string GenerateConsecutive(long nextNumber, string office, string terminal, string docType = "01")
        {
            // Estructura: CasaMatriz(3) + Terminal(5) + TipoDoc(2) + Consecutivo(10)
            return $"{office}{terminal}{docType}{nextNumber:D10}";
        }

        public static string GenerateKey(string taxId, string consecutive, string countryCode = "506")
        {
            var date = DateTime.Now;
            var day = date.Day.ToString("D2");
            var month = date.Month.ToString("D2");
            var year = date.Year.ToString().Substring(2, 2);
            
            // Limpiar taxId (solo números, 12 dígitos)
            var cleanTaxId = new string(taxId.Where(char.IsDigit).ToArray()).PadLeft(12, '0');
            
            var situation = "1"; // 1: Normal, 2: Contingencia, 3: Sin Internet
            var securityCode = new Random().Next(10000000, 99999999).ToString();
            
            // Clave: CódigoPaís(3) + Día(2) + Mes(2) + Año(2) + Identificación(12) + Consecutivo(20) + Situación(1) + CódigoSeguridad(8)
            return $"{countryCode}{day}{month}{year}{cleanTaxId}{consecutive}{situation}{securityCode}";
        }
    }
}
