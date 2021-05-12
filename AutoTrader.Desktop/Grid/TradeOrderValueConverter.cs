using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AutoTrader.Db.Entities;

namespace AutoTrader.Desktop.Grid
{
    public class TradeOrderValueConverter : IValueConverter
    {
        public DataTemplate TradeOrderOldTemplate { get; set; }
        public DataTemplate TradeOrderLossTemplate { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tradeOrder = value as TradeOrder;
            if (tradeOrder == null)
            {
                return Brushes.White;
            }

            if (tradeOrder.BuyDate.AddHours(1) < DateTime.Now)
            {
                return Brushes.OrangeRed;
            }
            if (tradeOrder.ActualYield < 0)
            {
                return Brushes.Yellow;
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
