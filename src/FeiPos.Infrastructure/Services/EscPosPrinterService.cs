using System;
using System.Text;
using System.Collections.Generic;
using FeiPos.Domain.Entities;
using FeiPos.Application.Interfaces;

namespace FeiPos.Infrastructure.Services
{
    public class EscPosPrinterService
    {
        public void PrintReceipt(Sale sale)
        {
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
            sb.Append(Center + BoldOn + DoubleHeight + "MI COMERCIO S.A.\n" + Reset);
            sb.Append(Center + "Cedula: 3-101-123456\n");
            sb.Append(Center + "Tel: 2222-3333\n");
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
                string line = $"{item.Quantity,-5} {item.ProductName.Substring(0, Math.Min(item.ProductName.Length, 15)),-15} {item.Total,10:N2}";
                sb.Append(Left + line + "\n");
            }
            sb.Append(new string('-', 32) + "\n");

            // Totales
            sb.Append(Left + "SUBTOTAL:" + sale.SubTotal.ToString("N2").PadLeft(23) + "\n");
            sb.Append(Left + "IVA:" + sale.TotalTax.ToString("N2").PadLeft(28) + "\n");
            sb.Append(Left + BoldOn + "TOTAL:" + sale.Total.ToString("N2").PadLeft(26) + "\n" + BoldOff);
            sb.Append(new string('-', 32) + "\n");

            // Pie Fiscal Hacienda
            sb.Append(Center + "CLAVE HACIENDA (50 DIGITOS):\n");
            sb.Append(Center + sale.HaciendaKey + "\n");
            sb.Append(Center + "Emitida por Fei POS - CR\n\n\n\n");

            // Corte de papel
            sb.Append(GS + "V" + (char)66 + (char)0);

            // Enviar a la impresora (Simulado por ahora para no bloquear el sistema)
            System.Diagnostics.Debug.WriteLine(sb.ToString());
        }
    }
}
