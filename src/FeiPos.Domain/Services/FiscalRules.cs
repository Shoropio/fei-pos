using System;

namespace FeiPos.Domain.Services
{
    public static class FiscalRules
    {
        public const decimal StandardIVA = 13.0m;
        public const decimal ReducedIVA_4 = 4.0m;
        public const decimal ReducedIVA_2 = 2.0m;
        public const decimal ReducedIVA_1 = 1.0m;
        public const decimal Exempt = 0.0m;

        public static decimal CalculateTax(decimal amount, decimal rate)
        {
            return Math.Round(amount * (rate / 100), 2, MidpointRounding.AwayFromZero);
        }

        public static string GetHaciendaCode(decimal rate)
        {
            return rate switch
            {
                StandardIVA => "08", // IVA Tasa Estándar 13%
                ReducedIVA_4 => "07", // IVA Tasa Reducida 4%
                ReducedIVA_2 => "06", // IVA Tasa Reducida 2%
                ReducedIVA_1 => "05", // IVA Tasa Reducida 1%
                _ => "01" // Exento
            };
        }
    }
}
