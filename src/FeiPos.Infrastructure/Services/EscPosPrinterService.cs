using System;
using System.Text;
using System.Collections.Generic;
using FeiPos.Domain.Entities;
using FeiPos.Application.Interfaces;

namespace FeiPos.Infrastructure.Services
{
    public class EscPosPrinterService
    {
        private readonly ConfigurationService _configService;

        public EscPosPrinterService(ConfigurationService configService)
        {
            _configService = configService;
        }

        public void PrintReceipt(Sale sale)
        {
            string printerName = _configService.Config.PrinterName;
            
            // Comandos ESC/POS básicos
            string ESC = "\u001B";
            string GS = "\u001D";
            string Center = ESC + "a" + "\u0001";
            string Left = ESC + "a" + "\u0000";
            string BoldOn = ESC + "E" + "\u0001";
            string BoldOff = ESC + "E" + "\u0000";
            string DoubleHeight = ESC + "!" + "\u0010";
            string Reset = ESC + "!";

            StringBuilder sb = new StringBuilder();
            
            // Encabezado
            sb.Append(Center + BoldOn + DoubleHeight + _configService.Config.CompanyName + "\n" + Reset);
            sb.Append(Center + "Cedula: " + _configService.Config.TaxId + "\n");
            sb.Append(Center + "San Jose, Costa Rica\n");
            sb.Append(new string('-', 32) + "\n");
            
            // Info Venta
            sb.Append(Left + "Factura: " + sale.ConsecutiveNumber + "\n");
            sb.Append(Left + "Fecha: " + sale.CreatedAt.ToString("g") + "\n");
            sb.Append(new string('-', 32) + "\n");

            // Detalle
            sb.Append(Left + BoldOn + "CANT  DESCRIPCION       TOTAL\n" + BoldOff);
            foreach (var item in sale.Items)
            {
                string name = item.ProductName.Length > 15 ? item.ProductName.Substring(0, 15) : item.ProductName;
                string line = $"{item.Quantity,-5} {name,-15} {item.Total,10:N2}";
                sb.Append(Left + line + "\n");
            }
            sb.Append(new string('-', 32) + "\n");

            // Totales
            sb.Append(Left + "SUBTOTAL:" + sale.SubTotal.ToString("N2").PadLeft(23) + "\n");
            sb.Append(Left + "IVA:" + sale.TotalTax.ToString("N2").PadLeft(28) + "\n");
            sb.Append(Left + BoldOn + "TOTAL:" + sale.Total.ToString("N2").PadLeft(26) + "\n" + BoldOff);
            sb.Append(new string('-', 32) + "\n");

            // Pie Fiscal Hacienda
            if (!string.IsNullOrEmpty(sale.HaciendaKey))
            {
                sb.Append(Center + "CLAVE HACIENDA (50 DIGITOS):\n");
                sb.Append(Center + sale.HaciendaKey + "\n");
            }
            sb.Append(Center + "Emitida por Fei POS - CR\n\n\n\n");

            // Corte de papel
            sb.Append(GS + "V" + (char)66 + (char)0);

            Helpers.RawPrinterHelper.SendStringToPrinter(printerName, sb.ToString());
        }

        public void PrintCashEntry(CashDrawerEntry entry)
        {
            string printerName = _configService.Config.PrinterName;
            string ESC = "\u001B";
            string Center = ESC + "a" + "\u0001";
            string Left = ESC + "a" + "\u0000";
            string BoldOn = ESC + "E" + "\u0001";
            string BoldOff = ESC + "E" + "\u0000";
            string Reset = ESC + "!";

            StringBuilder sb = new StringBuilder();
            sb.Append(Center + BoldOn + (entry.Type == CashDrawerEntryType.Deposit ? "COMPROBANTE DE DEPOSITO" : "COMPROBANTE DE RETIRO") + "\n" + Reset);
            sb.Append(new string('-', 32) + "\n");
            sb.Append(Left + "Fecha: " + entry.CreatedAt.ToString("g") + "\n");
            sb.Append(Left + "Usuario: " + entry.UserName + "\n");
            sb.Append(Left + "Razon: " + entry.Reason + "\n");
            sb.Append(new string('-', 32) + "\n");
            sb.Append(Left + BoldOn + "MONTO: ₡" + entry.Amount.ToString("N2").PadLeft(20) + "\n" + BoldOff);
            sb.Append(new string('-', 32) + "\n\n\n");
            sb.Append(Center + "____________________\n");
            sb.Append(Center + "Firma Recibido\n\n\n\n");
            sb.Append("\u001DV\u0042\u0000"); // Corte

            Helpers.RawPrinterHelper.SendStringToPrinter(printerName, sb.ToString());
        }

        public void PrintDayClosure(DayClosure closure)
        {
            string printerName = _configService.Config.PrinterName;
            string ESC = "\u001B";
            string Center = ESC + "a" + "\u0001";
            string Left = ESC + "a" + "\u0000";
            string BoldOn = ESC + "E" + "\u0001";
            string BoldOff = ESC + "E" + "\u0000";
            string Reset = ESC + "!";

            StringBuilder sb = new StringBuilder();
            sb.Append(Center + BoldOn + "CIERRE DE CAJA (CORTE Z)\n" + Reset);
            sb.Append(Center + "Fecha Negocio: " + closure.BusinessDate.ToShortDateString() + "\n");
            sb.Append(Center + "Cerrado el: " + closure.ClosedAt.ToString("g") + "\n");
            sb.Append(Center + "Usuario: " + closure.ClosedBy + "\n");
            sb.Append(new string('=', 32) + "\n");

            sb.Append(Left + "VENTAS TOTALES:" + closure.SalesTotal.ToString("N2").PadLeft(17) + "\n");
            sb.Append(new string('-', 32) + "\n");
            sb.Append(Left + "Efectivo:" + closure.CashTotal.ToString("N2").PadLeft(23) + "\n");
            sb.Append(Left + "Tarjeta:" + closure.CardTotal.ToString("N2").PadLeft(24) + "\n");
            sb.Append(Left + "Cheque:" + closure.CheckTotal.ToString("N2").PadLeft(25) + "\n");
            sb.Append(Left + "Credito:" + closure.CreditTotal.ToString("N2").PadLeft(24) + "\n");
            sb.Append(new string('-', 32) + "\n");
            sb.Append(Left + "Depósitos (+):" + closure.DepositsTotal.ToString("N2").PadLeft(18) + "\n");
            sb.Append(Left + "Retiros (-):" + closure.WithdrawalsTotal.ToString("N2").PadLeft(20) + "\n");
            sb.Append(new string('-', 32) + "\n");
            sb.Append(Left + BoldOn + "EFECTIVO ESPERADO:" + closure.ExpectedCash.ToString("N2").PadLeft(14) + "\n" + BoldOff);
            sb.Append(Left + "EFECTIVO CONTADO:" + closure.CountedCash.ToString("N2").PadLeft(15) + "\n");
            sb.Append(Left + BoldOn + "DIFERENCIA:" + closure.Difference.ToString("N2").PadLeft(21) + "\n" + BoldOff);
            sb.Append(new string('=', 32) + "\n\n\n\n");
            sb.Append("\u001DV\u0042\u0000"); // Corte

            Helpers.RawPrinterHelper.SendStringToPrinter(printerName, sb.ToString());
        }

        public void PrintReceiptCopy(Sale sale)
        {
            string printerName = _configService.Config.PrinterName;
            string ESC = "\u001B";
            string Center = ESC + "a" + "\u0001";
            
            // Reutilizamos la lógica de PrintReceipt pero añadimos marca de copia
            // Para ser eficientes en este prompt, solo enviamos una cabecera y llamamos a PrintReceipt (idealmente se refactoriza)
            string copyHeader = Center + "\u001BE\u0001*** COPIA DE RECIBO ***\u001BE\u0000\n";
            Helpers.RawPrinterHelper.SendStringToPrinter(printerName, copyHeader);
            PrintReceipt(sale);
        }

        public void OpenDrawer()
        {
            string printerName = _configService.Config.PrinterName;
            byte[] drawerCommand = new byte[] { 27, 112, 0, 25, 250 }; // ESC p 0 25 250
            Helpers.RawPrinterHelper.SendBytesToPrinter(printerName, drawerCommand);
        }

        public void PrintTest()
        {
            string printerName = _configService.Config.PrinterName;
            string testStr = "\u001Ba\u0001Prueba de Impresion Fei POS\n\n\u001DV\u0042\u0000";
            Helpers.RawPrinterHelper.SendStringToPrinter(printerName, testStr);
        }
    }
}
