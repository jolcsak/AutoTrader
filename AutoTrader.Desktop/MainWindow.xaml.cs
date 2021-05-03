using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AutoTrader.Db;
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

        protected static Store Store => Store.Instance;

        protected static TradeSetting TradeSettings => TradeSetting.Instance;

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

        protected override void OnInitialized(EventArgs e)
        {
            WpfLogger.Init(logWindow.Console, null, openedOrders, closedOrders, balance, currencies, graph, selectedCurrency, totalBalance);
            
            Store.Connect();
            Store.LoadSettings();

            SetSettings();

            traderThread = new TraderThread();
            Task.Run(traderThread.Trade);

            base.OnInitialized(e);
        }

        private void SetSettings()
        {
            TradeSettings.SetCanSave(false);

            minYield.Text = TradeSettings.MinSellYield.ToString();

            balanceVisible.IsChecked = TradeSettings.BalanceGraphVisible;
            pricesVisible.IsChecked = TradeSettings.PriceGraphVisible;
            smaVisible.IsChecked = TradeSettings.SmaGraphVisible;
            aoVisible.IsChecked = TradeSettings.AoGraphVisible;
            tendencyVisible.IsChecked = TradeSettings.TendencyGraphVisible;
            predicitionVisible.IsChecked = TradeSettings.AiPredicitionVisible;

            TradeSettings.SetCanSave(true);
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
                    Store.SaveSettings();
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
                Logger.SelectedTradeOrder = null;
                Logger.RefreshGraph(currentTrader);
            }
        }

        private void aoRatio_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AoAgent.Ratio = e.NewValue;
            if (CurrentTrader != null)
            {
                foreach (ITrader trader in TraderThread.Traders) {
                    trader.AoAgent.RefreshAll(trader.TargetCurrency);
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
            if (selectedTradeOrder?.Id != Logger.SelectedTradeOrder?.Id)
            {
                Logger.SelectedTradeOrder = selectedTradeOrder;
                Logger.RefreshGraph(CurrentTrader);
            }
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

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void Balance_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.BalanceGraphVisible = true;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Balance_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.BalanceGraphVisible = false;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Price_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.PriceGraphVisible = true;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Price_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.PriceGraphVisible = false;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Sma_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.SmaGraphVisible = true;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Sma_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.SmaGraphVisible = false;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Ao_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.AoGraphVisible = true;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Ao_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.AoGraphVisible = false;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Tendency_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.TendencyGraphVisible = true;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Tendency_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.TendencyGraphVisible = false;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Prediction_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.AiPredicitionVisible = true;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Prediction_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.AiPredicitionVisible = false;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }
    }
}
