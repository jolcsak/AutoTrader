using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AutoTrader.Db;
using AutoTrader.Db.Entities;
using AutoTrader.Log;
using AutoTrader.Traders;
using AutoTrader.Traders.Bots;
using Microsoft.Win32;
using Newtonsoft.Json;

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
                var currentTrader = selectedCurrency != null ? traderThread.GetTrader(selectedCurrency.Name) : null;
                TraderThread.CurrentTrader = currentTrader;
                return currentTrader;
            }
        }

        public MainWindow()
        {
            logWindow = new LogWindow(this);
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            WpfLogger.Init(logWindow.Console, null, openedOrders, closedOrders, balance, currencies, graph, selectedCurrency, totalBalance, projectedIncome, 
                dailyProfit, weeklyProfit, monthlyProfit, dailyFiatProfit, weeklyFiatProfit, monthlyFiatProfit,
                benchmarkIteration, maxBProfit
                );
            
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

            canBuy.IsChecked = TradeSettings.CanBuy;
            minYield.Text = TradeSettings.MinSellYield.ToString();
            balanceVisible.IsChecked = TradeSettings.BalanceGraphVisible;
            pricesVisible.IsChecked = TradeSettings.PriceGraphVisible;
            smaVisible.IsChecked = TradeSettings.SmaGraphVisible;
            aoVisible.IsChecked = TradeSettings.AoGraphVisible;
            tendencyVisible.IsChecked = TradeSettings.TendencyGraphVisible;
            predicitionVisible.IsChecked = TradeSettings.AiPredicitionVisible;
            rsiVisible.IsChecked = TradeSettings.RsiVisible;
            tradesVisible.IsChecked = TradeSettings.TradesVisible;
            smaBotEnabled.IsChecked = TradeSettings.SmaBotEnabled;
            rsiBotEnabled.IsChecked = TradeSettings.RsiBotEnabled;
            macdVisible.IsChecked = TradeSettings.MacdVisible;
            macdBotEnabled.IsChecked = TradeSettings.MacdBotEnabled;
            spikeBotEnabled.IsChecked = TradeSettings.SpikeBotEnabled;
            aiBotEnabled.IsChecked = TradeSettings.AiBotEnabled;

            TradeSettings.SetCanSave(true);
        }

        private void CanBuy_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.CanBuy = true;
            Store.SaveSettings();
        }

        private void CanBuy_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.CanBuy = false;
            Store.SaveSettings();
        }

        private void MinYield_TextChanged(object sender, TextChangedEventArgs e)
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

        private void currencies_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var currentTrader = CurrentTrader;
            if (currentTrader != null)
            {
                Logger.SelectedCurrency = currentTrader.TargetCurrency;
                Logger.SelectedTradeOrder = null;
                //CurrentTrader?.BotManager.Refresh();
                Logger.LogProjectedIncome(currentTrader);
                Logger.RefreshGraph(currentTrader);
            }
        }

        private void aoRatio_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //AoBot.Ratio = e.NewValue;
            if (CurrentTrader != null)
            {
                foreach (ITrader trader in TraderThread.Traders) {
                    trader.BotManager.Refresh();
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
            var result = MessageBox.Show("Are you sure?", "Sell", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                CurrentTrader?.SellAll(false);
                MessageBox.Show("All orders all sold.", "Sell", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SellAllProfitable_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure?", "Sell", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                CurrentTrader?.SellAll(true);
                MessageBox.Show("All profitable orders all sold.", "Sell", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void openedOrders_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var selectedTradeOrder = openedOrders?.SelectedItem as TradeOrder;
            Logger.SelectedCurrency = selectedTradeOrder?.Currency;
            if (selectedTradeOrder?.Id != Logger.SelectedTradeOrder?.Id)
            {
                Logger.SelectedTradeOrder = selectedTradeOrder;
                Logger.RefreshGraph(CurrentTrader);
            }
        }

        private void closedOrders_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var selectedTradeOrder = closedOrders?.SelectedItem as TradeOrder;
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

        private void Rsi_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.RsiVisible = true;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Rsi_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.RsiVisible = false;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Trades_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.TradesVisible = true;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Trades_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.TradesVisible = false;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void SmaBotEnabled_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.SmaBotEnabled = true;
            RefreshAllTraders();
        }

        private void SmaBotEnabled_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.SmaBotEnabled = false;
            RefreshAllTraders();
        }

        private void RsiBotEnabled_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.RsiBotEnabled = true;
            RefreshAllTraders();
        }

        private void RsiBotEnabled_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.RsiBotEnabled = false;
            RefreshAllTraders();
        }

        private void MacdBotEnabled_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.MacdBotEnabled = true;
            RefreshAllTraders();
        }

        private void MacdBotEnabled_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.MacdBotEnabled = false;
            RefreshAllTraders();
        }

        private void SpikeBotEnabled_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.SpikeBotEnabled = true;
            RefreshAllTraders();
        }

        private void SpikeBotEnabled_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.SpikeBotEnabled = false;
            RefreshAllTraders();
        }

        private void AiBotEnabled_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.AiBotEnabled = true;
            RefreshAllTraders();
        }

        private void AiBotEnabled_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.AiBotEnabled = false;
            RefreshAllTraders();
        }

        private void RefreshAllTraders()
        {
            Store.SaveSettings();
            CurrentTrader?.BotManager.Refresh();
            Logger.RefreshGraph(CurrentTrader);
            Logger.LogProjectedIncome(CurrentTrader);
        }

        private void BuyLong(object sender, RoutedEventArgs e)
        {
            Buy(sender, TradePeriod.Long);
        }

        private void BuyShort(object sender, RoutedEventArgs e)
        {
            Buy(sender, TradePeriod.Short);
        }

        private void Buy(object sender, TradePeriod period)
        {
            ITrader currencyTrader = traderThread.GetTrader(selectedCurrency.Content.ToString());

            if (currencyTrader != null)
            {
                double btcBalance = currencyTrader.RefreshBalance();
                var currency = (sender as Button).DataContext as Currency;
                if (btcBalance >= BtcTrader.MinBtcTradeAmount)
                {
                    TradeResult tradeResult = currencyTrader.Buy(BtcTrader.MinBtcTradeAmount, currencyTrader.ActualPrice, period, null);
                    if (tradeResult != TradeResult.ERROR)
                    {
                        currencyTrader.RefreshBalance();
                        Logger.LogTradeOrders(CurrentTrader.AllTradeOrders);

                        string message = tradeResult == TradeResult.DONE ? 
                            $"{BtcTrader.MinBtcTradeAmount} {currency.Name} bought at price {currencyTrader.ActualPrice.BuyPrice:N8}." :
                            $"{BtcTrader.MinBtcTradeAmount} {currency.Name} placed at price {currencyTrader.ActualPrice.BuyPrice:N8}.";

                        MessageBox.Show(message, "Sell", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Buy failed! See the log.", "Buy", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Not enough balance to buy!", "Buy", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void SellMarket(object sender, RoutedEventArgs e)
        {
            Sell(sender, true);
        }

        private void SellLimit(object sender, RoutedEventArgs e)
        {
            Sell(sender, false);
        }

        private void CancelLimit(object sender, RoutedEventArgs e)
        {
            var tradeOrder = (sender as Button).DataContext as TradeOrder;
            if (tradeOrder.State == TradeOrderState.OPEN_ENTERED || tradeOrder.State == TradeOrderState.ENTERED)
            {
                TradeOrderState cancelState = tradeOrder.State == TradeOrderState.OPEN_ENTERED ? TradeOrderState.CANCELLED : TradeOrderState.OPEN;

                if (CurrentTrader.CancelLimit(tradeOrder, cancelState))
                {
                    MessageBox.Show($"Order cancelled", "Cancel", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Order cancel failed! See the log.", "Cancel", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Order not cancelable!", "Cancel", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Sell(object sender, bool isMarket)
        {
            if (CurrentTrader != null)
            {
                var tradeOrder = (sender as Button).DataContext as TradeOrder;
                TradeResult tradeResult = CurrentTrader.Sell(tradeOrder.ActualPrice, tradeOrder, isMarket);
                if (tradeResult != TradeResult.ERROR)
                {
                    CurrentTrader.RefreshBalance();
                    Logger.LogTradeOrders(CurrentTrader.AllTradeOrders);
                    string message = tradeResult == TradeResult.DONE ? $"Order sold at price {tradeOrder.ActualPrice:N8}." : $"Order placed at price {tradeOrder.ActualPrice:N8}.";
                    MessageBox.Show(message, "Sell", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Sell failed! See the log.", "Sell", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Macd_Checked(object sender, RoutedEventArgs e)
        {
            TradeSettings.MacdVisible = true;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void Macd_Unchecked(object sender, RoutedEventArgs e)
        {
            TradeSettings.MacdVisible = false;
            Store.SaveSettings();
            Logger.RefreshGraph(CurrentTrader);
        }

        private void benchMarkMode_Checked(object sender, RoutedEventArgs e)
        {
            TradingBotManager.BenchmarkIteration = 0;
            TradingBotManager.IsBenchmarking = true;
        }

        private void benchMarkMode_Unchecked(object sender, RoutedEventArgs e)
        {
            TradingBotManager.BenchmarkIteration = 0;
            TradingBotManager.IsBenchmarking = false;
        }

        private void saveBenchmarkData_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog())
            {
                File.WriteAllText(saveFileDialog.FileName, JsonConvert.SerializeObject(BenchmarkBot.MaxBenchProfitData));
            }
        }
    }
}
