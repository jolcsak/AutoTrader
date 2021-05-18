using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AutoTrader.Db.Entities;
using AutoTrader.Log;
using AutoTrader.Traders;
using Trady.Analysis;
using Trady.Core.Infrastructure;

namespace AutoTrader.Desktop
{
    public class WpfLogger : ITradeLogger
    {
        private const string INFO = "INFO";
        private const string WARN = "WARNING";
        private const string ERR = "ERROR";

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

        private static readonly ObservableCollection<Currency> currencyList = new ObservableCollection<Currency>();
        private static readonly ObservableCollection<TradeOrder> openedOrdersData = new ObservableCollection<TradeOrder>();
        private static readonly ObservableCollection<TradeOrder> closedOrdersData = new ObservableCollection<TradeOrder>();

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

        public static void Init(TextBox consoleInstance, ScrollViewer consoleScrollInstance, DataGrid openedOrdersInstance, DataGrid closedOrdersInstance, Label balanceInstance, DataGrid currenciesInstance, Canvas graphInstance, Label selectedCurrencyInst, Label totalBalanceInstance, Label projectedIncome)
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
        }

        public void Info(string msg)
        {
            if (console != null)
            {
                Dispatcher?.BeginInvoke(() => console.AppendText(GetFormattedString(INFO, msg)));
                ScrollToEnd();
            }
        }

        public void Warn(string msg)
        {
            if (console != null)
            {
                Dispatcher?.BeginInvoke(() => console.AppendText(GetFormattedString(WARN, msg)));
                ScrollToEnd();
            }
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
                   foreach (var newOpenedOrder in traderOrders.Where(o => o.State == TradeOrderState.OPEN))
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

                   foreach (var newClosedOrder in traderOrders.Where(o => o.State == TradeOrderState.CLOSED && !closedOrdersData.Any(i => i.Id.Equals(o.Id))))
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
            if (trader == null)
            {
                return;
            }
            if (SelectedCurrency.Equals(trader.TargetCurrency))
            {
                Dispatcher?.Invoke(() => graph.Children.Clear());

                TradingBotManager botManager = trader.BotManager;
                DateProvider dateProvider = botManager.DateProvider;
                dateProvider.Width = graph.ActualWidth;

                if (TradeSettings.AoGraphVisible)
                {
                    new BarGraph(graph, dateProvider, botManager.PastPrices.Count, "Awesome Oscillator", botManager.Ao).Draw();
                }

                if (TradeSettings.TendencyGraphVisible)
                {
                    new ValueGraph(graph, dateProvider,  "Trend", botManager.Trend, botManager.Trend.Count, Colors.Orange, showPoints: false).Draw();
                }

                if (TradeSettings.AiPredicitionVisible)
                {
                }

                if (TradeSettings.PriceGraphVisible)
                {
                    new ValueGraph(graph, dateProvider, "Prices", botManager.PastPrices.Select(p => new AnalyzableTick<decimal?>(p.DateTime, p.Close)).ToList(), botManager.PastPrices.Count, Colors.DarkGray, showPoints: false).
                        Draw(null, 0, TradeSettings.TradesVisible ? botManager.Trades : null, TradeSettings.TradesVisible ? trader.TradeOrders : null);
                }
                if (TradeSettings.SmaGraphVisible)
                {
                    var ret = new ValueGraph(graph, dateProvider, "Fast Simple Moving Average", botManager.SmaFast, botManager.PastPrices.Count, Colors.Blue, showPoints: true).Draw();
                    new ValueGraph(graph, dateProvider, "Slow Simple Moving Average", botManager.SmaSlow, botManager.PastPrices.Count, Colors.LightBlue, showPoints: false).Draw(ret.Item1, ret.Item2);
                }

                if (TradeSettings.RsiVisible)
                {
                    new RsiSections(graph).Draw();
                    new ValueGraph(graph, dateProvider, "Relative Strength Index", botManager.Rsi, botManager.PastPrices.Count, Colors.Purple, showPoints: false).Draw();
                }

                if (TradeSettings.MacdVisible)
                {
                    var macdLine = new List<AnalyzableTick<decimal?>>();
                    var macdSignal = new List<AnalyzableTick<decimal?>>();

                    for (int i = 0; i < botManager.PastPrices.Count; i++)
                    {
                        if (botManager.Macd[i]?.Tick != null) {
                            macdLine.Add(new AnalyzableTick<decimal?>(botManager.Macd[i].DateTime, botManager.Macd[i].Tick.MacdLine));
                            macdSignal.Add(new AnalyzableTick<decimal?>(botManager.Macd[i].DateTime, botManager.Macd[i].Tick.SignalLine));
                        }
                    }

                    new BarGraph(graph, dateProvider, botManager.PastPrices.Count, "MACD Histogram", botManager.MacdHistogram).Draw();

                    var ret = new ValueGraph(graph, dateProvider, "MACD Line", macdLine, botManager.PastPrices.Count, Colors.Orange, showPoints: false).Draw();
                    new ValueGraph(graph, dateProvider, "MACD Signal", macdSignal, botManager.PastPrices.Count, Colors.DarkViolet, showPoints: false).Draw(ret.Item1, ret.Item2);
                }

                new DateGraph(graph, dateProvider, botManager.Dates).Draw();

                if (botManager.FiatBalances.Count > 0)
                {
                    if (TradeSettings.BalanceGraphVisible)
                    {
                        new Graph(graph, "Total FIAT balance", botManager.FiatBalances, Colors.Olive, showPoints: false, "N0", 3).Draw();
                        new Graph(graph, "Total BTC balance", botManager.BtcBalances, Colors.DarkOliveGreen, showPoints: false, "N8", 2).Draw();
                    }
                    Dispatcher?.BeginInvoke(() => totalBalanceText.Content = botManager.FiatBalances.Last().ToString("N1") + " HUF");
                }
                else
                {
                    Dispatcher?.BeginInvoke(() => totalBalanceText.Content = "N/A");
                }

                if (SelectedTradeOrder != null)
                {
                    new PriceLine(graph, "Selected price", botManager.PastPrices.Select(pp => pp.Close), (decimal)SelectedTradeOrder.Price, Colors.Brown).Draw();
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
                Dispatcher?.Invoke(() => projectedIncomeText.Content = (100 * projectedIncome - 100).ToString("N4") + " %");
            }
        }
    }
}
