using AutoTrader.Db.Entities;
using AutoTrader.GraphProviders;
using AutoTrader.Log;
using AutoTrader.Traders;
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

        protected ITrader CurrentTrader
        {
            get
            {
                var selectedCurrency = currencies?.SelectedItem as Currency;
                return selectedCurrency != null ? traderThread.GetTrader(selectedCurrency.Name) : null;
            }
        }

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

        private void currencies_SelectedCellsChanged(object sender, System.Windows.Controls.SelectedCellsChangedEventArgs e)
        {
            var currentTrader = CurrentTrader;
            if (currentTrader != null)
            {
                Logger.RefreshGraph(currentTrader);
            }
        }

        private void aoRatio_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AoProvider.Ratio = e.NewValue;
            if (CurrentTrader != null)
            {
                CurrentTrader.GraphCollection.AoProvider.RefreshAll();
                currencies_SelectedCellsChanged(sender, null);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            currencies_SelectedCellsChanged(sender, null);
        }
    }
}
