using System;
using System.Globalization;
using System.Windows.Data;

namespace FeiPos.Presentation.Converters
{
    public class BusinessTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Cash" => "Efectivo",
                "Card" => "Tarjeta",
                "Check" => "Cheque",
                "Credit" => "Credito",
                "Deposit" => "Deposito",
                "Withdrawal" => "Retiro",
                "Draft" => "Abierta",
                "Finalized" => "Finalizada",
                "Cancelled" => "Anulada",
                var text => text ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
