using AutoTrader.Db.Entities;
using AutoTrader.Log;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace AutoTrader.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TraderThread traderThread;

        protected ITradeLogger Logger => TradeLogManager.GetLogger(string.Empty);

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
        }

        protected override void OnInitialized(EventArgs e)
        {
            WpfLogger.Init(null, null, openedOrders, closedOrders, balance, currencies, graph, selectedCurrency);

            SetRatios();

            traderThread = new TraderThread();
            Task.Run(traderThread.Trade);

            base.OnInitialized(e);
        }

        private void SetRatios()
        {
            buyRatio.Text = TradeSettings.BuyRatio.ToString();
            sellRatio.Text = TradeSettings.SellRatio.ToString();
            minYield.Text = TradeSettings.MinSellYield.ToString();
        }

        private void CanBuy_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.CanBuy = true;
        }

        private void CanBuy_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.CanBuy = false;
        }

        private void MinYield_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            double minSellYieldValue;
            if (double.TryParse(minYield.Text, out minSellYieldValue))
            {
                if (minSellYieldValue >= 1 && minSellYieldValue < 100)
                {
                    TradeSettings.MinSellYield = minSellYieldValue;
                    minYield.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("White"));
                }
                else
                {
                    minYield.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("Red"));
                }
            }
            else
            {
                minYield.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("Red"));
            }
        }

        private void BuyRatio_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            double buyRatioValue;
            if (double.TryParse(buyRatio.Text, out buyRatioValue))
            {
                if (buyRatioValue > 0 && buyRatioValue < 5)
                {
                    TradeSettings.BuyRatio = buyRatioValue;
                    buyRatio.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("White"));
                }
                else
                {
                    buyRatio.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("Red"));
                }
            }
            else
            {
                buyRatio.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("Red"));
            }
        }

        private void SellRatio_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            double sellRatioValue;
            if (double.TryParse(sellRatio.Text, out sellRatioValue))
            {
                if (sellRatioValue > 0 && sellRatioValue < 5)
                {
                    TradeSettings.SellRatio = sellRatioValue;
                    sellRatio.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("White"));
                }
                else
                {
                    sellRatio.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("Red"));
                }
            }
            else
            {
                sellRatio.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("Red"));
            }
        }

        private void currencies_SelectedCellsChanged(object sender, System.Windows.Controls.SelectedCellsChangedEventArgs e)
        {
            var selectedCurrency = currencies.SelectedItem as Currency;
            if (selectedCurrency != null)
            {
                var traderForCurrency = traderThread.GetTrader(selectedCurrency.Name);
                if (traderForCurrency != null)
                {
                    Logger.LogAo(selectedCurrency.Name, traderForCurrency.Ao);
                    Logger.LogPastPrices(selectedCurrency.Name, traderForCurrency.PastPrices, traderForCurrency.PastPricesSkip);
                    Logger.LogSma(selectedCurrency.Name, traderForCurrency.Sma, traderForCurrency.SmaSkip);
                }
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            currencies_SelectedCellsChanged(sender, null);
        }
    }
}
