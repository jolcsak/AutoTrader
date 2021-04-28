using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AutoTrader.Db.Entities;
using AutoTrader.Log;
using AutoTrader.Traders;
using AutoTrader.Traders.Agents;

namespace AutoTrader.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TraderThread traderThread;

        private LogWindow logWindow = null;

        public bool IsLogWindowClosed { get; set; } = true;

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
            logWindow = new LogWindow(this);
            InitializeComponent();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
        }

        protected override void OnInitialized(EventArgs e)
        {
            WpfLogger.Init(logWindow.Console, null, openedOrders, closedOrders, balance, currencies, graph, selectedCurrency, totalBalance);

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
                Logger.SelectedCurrency = currentTrader.TargetCurrency;
                Logger.RefreshGraph(currentTrader);
            }
        }

        private void aoRatio_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AoAgent.Ratio = e.NewValue;
            if (CurrentTrader != null)
            {
                foreach (ITrader trader in TraderThread.Traders) {
                    trader.AoAgent.RefreshAll();
                }
                currencies_SelectedCellsChanged(sender, null);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            currencies_SelectedCellsChanged(sender, null);
        }

        private void SellAll_Click(object sender, RoutedEventArgs e)
        {
            CurrentTrader?.SellAll(false);
        }

        private void SellAllProfitable_Click(object sender, RoutedEventArgs e)
        {
            CurrentTrader?.SellAll(true);
        }

        private void openedOrders_SelectedCellsChanged(object sender, System.Windows.Controls.SelectedCellsChangedEventArgs e)
        {
            var selectedTradeOrder = openedOrders?.SelectedItem as TradeOrder;
            Logger.SelectedCurrency = selectedTradeOrder?.Currency;
        }

        private void showLog_Click(object sender, RoutedEventArgs e)
        {
            if (IsLogWindowClosed || logWindow == null)
            {
                logWindow = new LogWindow(this);
                WpfLogger.SetConsole(logWindow.console);
            }
            logWindow.Show();
            logWindow.Focus();
        }
    }
}
