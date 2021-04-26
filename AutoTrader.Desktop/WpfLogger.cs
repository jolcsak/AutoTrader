using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AutoTrader.Db.Entities;
using AutoTrader.Log;
using AutoTrader.Traders;
using MathNet.Filtering;
using MathNet.Filtering.FIR;

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
        private static Label selectedCurrencyLabel;

        private static IList<Currency> currencyList = new List<Currency>();

        private static string selectedCurrency;

        protected string Name { get; private set; }

        protected Dispatcher Dispatcher => Application.Current != null ? Application.Current.Dispatcher : null;

        public static void Init(TextBox consoleInstance, ScrollViewer consoleScrollInstance, DataGrid openedOrdersInstance, DataGrid closedOrdersInstance, Label balanceInstance, DataGrid currenciesInstance, Canvas graphInstance, Label selectedCurrencyInst)
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
            return DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff") + $" - {level} - {Name} : {message}" + Environment.NewLine;
        }

        private void ScrollToEnd()
        {
            Dispatcher?.BeginInvoke(() => {
                console.CaretIndex = console.Text.Length;
                console.ScrollToEnd();
                consoleScroll.ScrollToEnd();
            });
        }

        public void LogTradeOrders(IList<TradeOrder> traderOrders)
        {
            Dispatcher?.BeginInvoke(() => {
                openedOrders.ItemsSource = traderOrders.Where(o => o.Type == TradeOrderType.OPEN).OrderByDescending(o => o.ActualYield);
                closedOrders.ItemsSource = traderOrders.Where(o => o.Type == TradeOrderType.CLOSED).OrderByDescending(o => o.SellDate);
            });
        }

        public void LogBalance(string currency, double balance)
        {
            Dispatcher?.BeginInvoke(() => balanceText.Content = $"{balance:N10} {currency}");
        }

        public void LogCurrency(ITrader trader, double price, double amount)
        {
            if (currencies.ItemsSource == null)
            {
                Dispatcher?.BeginInvoke(() => currencies.ItemsSource = currencyList);
            }

            string currency = trader.TargetCurrency;

            var currencyInst = currencyList.FirstOrDefault(c => c.Name == currency);
            if (currencyInst == null)
            {
                currencyInst = new Currency { Name = currency, Price = price, Amount = amount, Frequency = trader.Frequency, Amplitude = trader.Amplitude, Order = trader.Order, LastUpdate = trader.LastPriceDate };
                currencyList.Add(currencyInst);
                RefreshCurrencyList();
            }
            else
            {
                if (currencyInst.Refresh(price, amount, trader.GraphCollection.MinPeriodPrice, trader.GraphCollection.MaxPeriodPrice, trader.Frequency, trader.Amplitude, trader.Order, trader.LastPriceDate))
                {
                    RefreshCurrencyList();
                }
            }

            if (selectedCurrency != currency)
            {
                return;
            }
            RefreshGraph(trader);
        }

        public void RefreshGraph(ITrader trader)
        {
            Dispatcher?.Invoke(() => graph.Children.Clear());

            GraphCollection graphCollection = trader.GraphCollection;
            graphCollection.Refresh();
            new BarGraph(graph, "Awesome Oscillator", graphCollection.Ao, Colors.Yellow, Colors.Blue).Draw();

            //Complex[] signal = graphCollection.PastPrices.Select(val => new Complex(val, 0)).ToArray();
            //Fourier.Inverse(signal, FourierOptions.Matlab);
            //var result = signal.Select(x => new AoValue { Value = x.Magnitude, Color = AoColor.Green }).Take(signal.Length / 2).Skip(3).ToArray();
            //new BarGraph(graph, "FFT", result, Colors.LightBlue, Colors.Orange).Draw();

            var filter = OnlineFirFilter.CreateLowpass(ImpulseResponse.Finite, 50, trader.Amplitude);
            var result2 = filter.ProcessSamples(graphCollection.PastPrices.ToArray());
            new Graph(graph, "Price low-pass", result2, Colors.Orange, showPoints: false).Draw(85);

            new Graph(graph, "BTC Price ratio", graphCollection.PastPrices, Colors.DarkGray, showPoints: true).Draw(graphCollection.PricesSkip);
            new Graph(graph, "Simple Moving Average", graphCollection.Sma, Colors.Blue, showPoints: false).Draw(graphCollection.SmaSkip);

            new DateGraph(graph, graphCollection.Dates).Draw(graphCollection.PricesSkip);
        }

        private void RefreshCurrencyList()
        {
            Dispatcher?.BeginInvoke(() =>
            {
                var selectedItem = currencies.SelectedItem;
                currencies.Items.Refresh();
                if (selectedItem != null)
                {
                    currencies.SelectedItem = selectedItem;
                    currencies.ScrollIntoView(selectedItem);
                    var row = (DataGridRow)currencies.ItemContainerGenerator.ContainerFromIndex(currencies.SelectedIndex);
                    row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            });
        }
    }
}
