using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AutoTrader.Db.Entities;
using AutoTrader.Log;
using AutoTrader.Traders;
using Trady.Analysis;
using Trady.Analysis.Indicator;

namespace AutoTrader.Desktop
{
    public class WpfLogger : ITradeLogger
    {
        private const string LogPath = @"C:\Temp";
        private const string LogFolder = "AutoTrader";
        private const string LogFile = "AutoTrader.Log";

        private const string INFO = "INFO";
        private const string WARN = "WARNING";
        private const string ERR = "ERROR";

        public const int RSI_PERIOD = 14;
        public const int EMA_PERIOD = 48;

        private const int SMA_FAST_SMOOTHNESS = 5;
        private const int SMA_SLOW_SMOOTHNESS = 9;

        private const int EMA_FAST = 12;
        private const int EMA_SLOW = 26;
        private const int MACD_SIGNAL = 9;

        private static TextBox console;
        private static ScrollViewer consoleScroll;
        private static DataGrid openedOrders;
        private static DataGrid closedOrders;
        private static Label balanceText;
        private static DataGrid currencies;
        private static Canvas graph;

        private static string selectedCurrency = string.Empty;
        private static Label selectedCurrencyLabel;
        private static Label totalBalanceText;
        private static TradeOrder selectedTradeOrder;
        private static Label projectedIncomeText;

        private static Label dailyProfitLabel;
        private static Label weeklyProfitLabel;
        private static Label monthlyProfitLabel;

        private static Label dailyFiatProfitLabel;
        private static Label weeklyFiatProfitLabel;
        private static Label monthlyFiatProfitLabel;

        private static Label benchmarkIterationLabel;
        private static Label benchProfitLabel;

        private static readonly ObservableCollection<Currency> currencyList = new ObservableCollection<Currency>();
        private static readonly ObservableCollection<TradeOrder> openedOrdersData = new ObservableCollection<TradeOrder>();
        private static readonly ObservableCollection<TradeOrder> closedOrdersData = new ObservableCollection<TradeOrder>();

        private static string LogFilePath;

        public SimpleMovingAverage SmaSlow { get; private set; }
        public SimpleMovingAverage SmaFast { get; private set; }

        public SimpleMovingAverageOscillator Ao { get; private set; }

        public RelativeStrengthIndex Rsi { get; private set; }

        public MovingAverageConvergenceDivergence Macd { get; private set; }

        public MovingAverageConvergenceDivergenceHistogram MacdHistogram { get; private set; }

        public ExponentialMovingAverage Ema24 { get; private set; }
        public ExponentialMovingAverage Ema48 { get; private set; }

        public ExponentialMovingAverage Ema100 { get; private set; }

        protected TradeSetting TradeSettings => TradeSetting.Instance;

        public TradeOrder SelectedTradeOrder 
        {
            get => selectedTradeOrder;
            set => selectedTradeOrder = value;
        }

        public string SelectedCurrency
        {
            get => selectedCurrency;
            set
            {
                if (selectedCurrency != value)
                {
                    selectedCurrency = value;
                    selectedCurrencyLabel.Content = value;
                    var selectedItem = currencyList.FirstOrDefault(c => c.Name.Equals(value));
                    currencies.SelectedItem = selectedItem;
                    if (selectedItem != null)
                    {
                        currencies.ScrollIntoView(currencies.SelectedItem);
                    }
                    else
                    {
                        selectedCurrency = string.Empty;
                        selectedCurrencyLabel.Content = "-";
                    }
                }
            }
        }

        protected string Name { get; private set; }

        protected Dispatcher Dispatcher => Application.Current != null ? Application.Current.Dispatcher : null;

        public static void Init(TextBox consoleInstance, ScrollViewer consoleScrollInstance, DataGrid openedOrdersInstance, DataGrid closedOrdersInstance, Label balanceInstance, 
                                DataGrid currenciesInstance, Canvas graphInstance, Label selectedCurrencyInst, Label totalBalanceInstance, Label projectedIncome,
                                Label dailyProfit, Label weeklyProfit, Label monthlyProfit,
                                Label dailyFiatProfit, Label weeklyFiatProfit, Label monthlyFiatProfit,
                                Label benchmarkIteration, Label benchProfit
                                )
        {
            TradeLogManager.Init(new WpfLogger(string.Empty));
            console = consoleInstance;
            consoleScroll = consoleScrollInstance;
            openedOrders = openedOrdersInstance;
            closedOrders = closedOrdersInstance;
            balanceText = balanceInstance;
            currencies = currenciesInstance;
            graph = graphInstance;
            selectedCurrencyLabel = selectedCurrencyInst;
            totalBalanceText = totalBalanceInstance;
            projectedIncomeText = projectedIncome;

            currencies.ItemsSource = currencyList;
            openedOrders.ItemsSource = openedOrdersData;
            closedOrders.ItemsSource = closedOrdersData;

            dailyProfitLabel = dailyProfit;
            weeklyProfitLabel = weeklyProfit;
            monthlyProfitLabel = monthlyProfit;

            dailyFiatProfitLabel = dailyFiatProfit;
            weeklyFiatProfitLabel = weeklyFiatProfit;
            monthlyFiatProfitLabel = monthlyFiatProfit;

            benchmarkIterationLabel = benchmarkIteration;
            benchProfitLabel = benchProfit;

            InitLogFile();
        }

        private static void InitLogFile()
        {
            if (Directory.Exists(LogPath))
            {
                string fullLogPath = Path.Combine(LogPath, LogFolder);
                if (!Directory.Exists(fullLogPath))
                {
                    Directory.CreateDirectory(fullLogPath);
                }
                LogFilePath = Path.Combine(fullLogPath, LogFile);
            }
        }

        private static void WriteToLogFile(string message)
        {
            if (LogFilePath != null)
            {
                try
                {
                    File.AppendAllText(LogFilePath, message + Environment.NewLine);
                }
                catch { }
            }
        }

        public static void SetConsole(TextBox consoleInstance)
        {
            console = consoleInstance;
        }

        protected WpfLogger(string name)
        {
            Name = name;
        }

        public ITradeLogger GetLogger(string name)
        {
            return new WpfLogger(name);
        }

        public void Err(string msg)
        {
            if (console != null)
            {
                Dispatcher?.BeginInvoke(() => console.AppendText(GetFormattedString(ERR, msg)));
                ScrollToEnd();
            }
            WriteToLogFile(msg);
        }

        public void Info(string msg)
        {
            if (console != null)
            {
                Dispatcher?.BeginInvoke(() => console.AppendText(GetFormattedString(INFO, msg)));
                ScrollToEnd();
            }
            WriteToLogFile(msg);
        }

        public void Warn(string msg)
        {
            if (console != null)
            {
                Dispatcher?.BeginInvoke(() => console.AppendText(GetFormattedString(WARN, msg)));
                ScrollToEnd();
            }
            WriteToLogFile(msg);
        }

        private string GetFormattedString(string level, string message)
        {
            return $"{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff")} - {level} - {Name} : {message}{Environment.NewLine}";
        }

        private void ScrollToEnd()
        {
            Dispatcher?.BeginInvoke(() => {
                console.CaretIndex = console.Text.Length;
                console.ScrollToEnd();
                consoleScroll?.ScrollToEnd();
            });
        }

        public void LogTradeOrders(IList<TradeOrder> traderOrders)
        {
            Dispatcher?.BeginInvoke(() =>
               {
                   foreach (var newOpenedOrder in traderOrders.Where(o => o.State == TradeOrderState.OPEN || o.State == TradeOrderState.OPEN_ENTERED || o.State == TradeOrderState.ENTERED))
                   {
                       var existingOrder = openedOrdersData.FirstOrDefault(i => i.Id.Equals(newOpenedOrder.Id));
                       if (existingOrder == null)
                       {
                           openedOrdersData.Add(newOpenedOrder);
                       }
                       else
                       {
                           existingOrder.RefreshFrom(newOpenedOrder);
                       }
                   }

                   foreach (var newClosedOrder in traderOrders.Where(o => (o.State == TradeOrderState.CLOSED) && !closedOrdersData.Any(i => i.Id.Equals(o.Id))))
                   {
                       var existingOpenedOrdersData = openedOrdersData.FirstOrDefault(oe => oe.Id == newClosedOrder.Id);
                       if (existingOpenedOrdersData != null)
                       {
                           openedOrdersData.Remove(existingOpenedOrdersData);
                       }
                       closedOrdersData.Add(newClosedOrder);
                   }
               });
        }

        public void LogBalance( double balance)
        {
            Dispatcher?.BeginInvoke(() => balanceText.Content = $"{balance:N8}");
        }

        public void LogCurrency(ITrader trader, ActualPrice actualPrice)
        {
            string currency = trader.TargetCurrency;

            var currencyInst = currencyList.FirstOrDefault(c => c.Name == currency);
            Dispatcher?.BeginInvoke(() =>
               {
                   if (currencyInst == null)
                   {
                       currencyInst = new Currency { Name = currency, BuyPrice = actualPrice.BuyPrice, BuyAmount = actualPrice.BuyAmount, SellPrice = actualPrice.SellPrice, SellAmount = actualPrice.SellAmount, Order = trader.Order, LastUpdate = trader.LastPriceDate };
                       currencyList.Add(currencyInst);
                   }
                   else
                   {
                       currencyInst.Refresh(actualPrice, trader.Order, trader.LastPriceDate);
                   }
               });

            RefreshGraph(trader);
        }

        public void RefreshGraph(ITrader trader)
        {
            if (trader?.BotManager?.DateProvider == null || trader?.BotManager?.Prices == null)
            {
                return;
            }
            if (SelectedCurrency.Equals(trader.TargetCurrency))
            {
                TradingBotManager botManager = trader.BotManager;

                IList<Trady.Core.Infrastructure.IOhlcv> prices = botManager.Prices;
                int priceCount = prices.Count;

                SmaSlow = TradeSettings.SmaGraphVisible ? new SimpleMovingAverage(prices, SMA_SLOW_SMOOTHNESS) : null;
                SmaFast = TradeSettings.SmaGraphVisible ? new SimpleMovingAverage(prices, SMA_FAST_SMOOTHNESS) : null;
                Ao = TradeSettings.AoGraphVisible ? new SimpleMovingAverageOscillator(prices, SMA_FAST_SMOOTHNESS, SMA_SLOW_SMOOTHNESS) : null;
                Rsi = TradeSettings.RsiVisible ? new RelativeStrengthIndex(prices, RSI_PERIOD) : null;
                Macd = TradeSettings.MacdVisible ? new MovingAverageConvergenceDivergence(prices, EMA_FAST, EMA_SLOW, MACD_SIGNAL) : null;
                MacdHistogram = TradeSettings.MacdVisible ? new MovingAverageConvergenceDivergenceHistogram(prices, EMA_FAST, EMA_SLOW, MACD_SIGNAL) : null;
                Ema24 = TradeSettings.TendencyGraphVisible ? new ExponentialMovingAverage(prices, 24) : null;
                Ema48 = TradeSettings.TendencyGraphVisible ? new ExponentialMovingAverage(prices, 48) : null;
                Ema100 = TradeSettings.TendencyGraphVisible ? new ExponentialMovingAverage(prices, 100) : null;

                Dispatcher?.Invoke(() => graph.Children.Clear());

                DateProvider dateProvider = botManager.DateProvider;
                dateProvider.Width = graph.ActualWidth;

                if (TradeSettings.AoGraphVisible)
                {
                    new BarGraph(graph, dateProvider, priceCount, "Awesome Oscillator", Ao).Draw();
                }

                if (TradeSettings.TendencyGraphVisible)
                {
                    var ret = new ValueLine(graph, dateProvider, "EMA24", Ema24, priceCount, Colors.Blue, showPoints: true).Draw();
                    new ValueLine(graph, dateProvider, "EMA48", Ema48, priceCount, Colors.Magenta, showPoints: true).Draw(ret.Item1, ret.Item2);
                    new ValueLine(graph, dateProvider, "EMA100", Ema100, priceCount, Colors.DodgerBlue, showPoints: true).Draw(ret.Item1, ret.Item2);
                }

                if (TradeSettings.AiPredicitionVisible)
                {
                }

                if (TradeSettings.PriceGraphVisible)
                {
                    new ValueLine(graph, dateProvider, "Prices", prices.Select(p => new AnalyzableTick<decimal?>(p.DateTime, p.Close)).ToList(), priceCount, Colors.DarkGray, showPoints: false).
                        Draw(null, 0, TradeSettings.TradesVisible ? botManager.Trades : null, TradeSettings.TradesVisible ? trader.TradeOrders : null);
                }
                if (TradeSettings.SmaGraphVisible)
                {
                    var ret = new ValueLine(graph, dateProvider, "Fast Simple Moving Average", SmaFast, priceCount, Colors.Blue, showPoints: true).Draw();
                    new ValueLine(graph, dateProvider, "Slow Simple Moving Average", SmaSlow, priceCount, Colors.LightBlue, showPoints: false).Draw(ret.Item1, ret.Item2);
                }

                if (TradeSettings.RsiVisible)
                {
                    new RsiSections(graph).Draw();
                    new ValueLine(graph, dateProvider, "Relative Strength Index", Rsi, priceCount, Colors.Purple, showPoints: false).Draw();
                }

                if (TradeSettings.MacdVisible)
                {
                    var macdLine = new List<AnalyzableTick<decimal?>>();
                    var macdSignal = new List<AnalyzableTick<decimal?>>();

                    for (int i = 0; i < priceCount; i++)
                    {
                        if (Macd[i]?.Tick != null) {
                            macdLine.Add(new AnalyzableTick<decimal?>(Macd[i].DateTime, Macd[i].Tick.MacdLine));
                            macdSignal.Add(new AnalyzableTick<decimal?>(Macd[i].DateTime, Macd[i].Tick.SignalLine));
                        }
                    }

                    new BarGraph(graph, dateProvider, priceCount, "MACD Histogram", MacdHistogram).Draw();

                    var ret = new ValueLine(graph, dateProvider, "MACD Line", macdLine, priceCount, Colors.Orange, showPoints: false).Draw();
                    new ValueLine(graph, dateProvider, "MACD Signal", macdSignal, priceCount, Colors.DarkViolet, showPoints: false).Draw(ret.Item1, ret.Item2);
                }

                new DateGraph(graph, dateProvider, botManager.Dates).Draw();

                if (TradingBotManager.FiatBalances.Count > 0)
                {
                    if (TradeSettings.BalanceGraphVisible)
                    {
                        new Graph(graph, "Total FIAT balance", TradingBotManager.FiatBalances, Colors.Olive, showPoints: false, "N0", 3).Draw();
                        new Graph(graph, $"Total {BtcTrader.BTC} balance", TradingBotManager.BtcBalances, Colors.DarkOliveGreen, showPoints: false, "N8", 2).Draw();
                    }
                    Dispatcher?.BeginInvoke(() => totalBalanceText.Content = TradingBotManager.FiatBalances.Last().ToString("N1") + " HUF");
                }
                else
                {
                    Dispatcher?.BeginInvoke(() => totalBalanceText.Content = "N/A");
                }

                if (SelectedTradeOrder != null)
                {
                    //new PriceLine(graph, "Selected price", prices.Select(pp => pp.Close), (decimal)SelectedTradeOrder.Price, Colors.Brown).Draw();
                }
            }
        }

        public void LogProjectedIncome(ITrader trader)
        {
            if (trader == null)
            {
                return;
            }
            if (SelectedCurrency.Equals(trader.TargetCurrency))
            {
                double projectedIncome = trader.BotManager.ProjectedIncome;
                Dispatcher?.Invoke(() => projectedIncomeText.Content = projectedIncome.ToString("N1") + " %");
            }
        }

        public void LogProfit(double daily, double weekly, double monthly)
        {
            Dispatcher?.BeginInvoke(() =>
            {
                dailyProfitLabel.Content = daily.ToString("N1") + "%";
                weeklyProfitLabel.Content = weekly.ToString("N1") + "%";
                monthlyProfitLabel.Content = monthly.ToString("N1") + "%";
            });
        }

        public void LogFiatProfit(double daily, double weekly, double monthly)
        {
            Dispatcher?.BeginInvoke(() =>
            {
                dailyFiatProfitLabel.Content = daily.ToString("N0") + " HUF";
                weeklyFiatProfitLabel.Content = weekly.ToString("N0") + " HUF";
                monthlyFiatProfitLabel.Content = monthly.ToString("N0") + " HUF";
            });
        }

        public void LogBenchmarkIteration(int iteration, double benchProfit, double storedProfit)
        {
            Dispatcher.BeginInvoke(() => benchmarkIterationLabel.Content = iteration.ToString());
            Dispatcher.BeginInvoke(() => benchProfitLabel.Content = $"{benchProfit.ToString("N0")} ({storedProfit.ToString("N0")})");
        }
    }
}
