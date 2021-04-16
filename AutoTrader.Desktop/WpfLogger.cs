using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AutoTrader.Db;
using AutoTrader.Db.Entities;
using AutoTrader.GraphProviders;
using AutoTrader.Log;

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
        private static IList<double> currentPastPrices;
        private static IList<double> currentSma;
        private static IList<AoValue> currentAo;

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

        public void LogTradeOrders(IList<TradeOrder> traderOrders, string currency, double actualPrice)
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

        public void LogCurrency(string currency, double price, double amount, double minPeriodPrice, double maxPeriodPrice, double buyRatio, double sellRatio, int smaSkip, int pricesSkip)
        {
            if (currencies.ItemsSource == null)
            {
                Dispatcher?.BeginInvoke(() => currencies.ItemsSource = currencyList);
            }

            var currencyInst = currencyList.FirstOrDefault(c => c.Name == currency);
            if (currencyInst == null)
            {
                currencyInst = new Currency { Name = currency, Price = price, Amount = amount };
                currencyList.Add(currencyInst);
                RefreshCurrencyList();
            } else
            {
                if (currencyInst.Refresh(price, amount, minPeriodPrice, maxPeriodPrice, buyRatio, sellRatio))
                {
                    RefreshCurrencyList();
                }
            }

            RefreshPrices(currency, pricesSkip);
            RefreshSma(currency, smaSkip);
            RefreshAo(currency);
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

        public void LogPastPrices(string currency, IList<double> pastPrices, int pricesSkip)
        {
            currentPastPrices = pastPrices;
            selectedCurrency = currency;
            Dispatcher?.BeginInvoke(() => selectedCurrencyLabel.Content = currency);
            RefreshPrices(currency, pricesSkip);
        }

        public void LogSma(string currency, IList<double> sma, int smaSkip)
        {
            currentSma = sma;
            selectedCurrency = currency;
            RefreshSma(currency, smaSkip);
        }

        public void LogAo(string currency, IList<AoValue> ao)
        {
            currentAo = ao;
            selectedCurrency = currency;
            RefreshAo(currency);
        }

        public void RefreshPrices(string currency, int pricesSkip)
        {
            if (selectedCurrency != currency)
            {
                return;
            }
            Dispatcher?.Invoke(() => graph.Children.Clear());

            new Graph(graph, "BTC Price ratio", currentPastPrices, Colors.DarkGray, showPoints: false).Draw(pricesSkip);
        }

        public void RefreshSma(string currency, int smaSkip)
        {
            if (selectedCurrency != currency)
            {
                return;
            }
            new Graph(graph, "Simple Moving Average", currentSma, Colors.Blue, showPoints: false).Draw(smaSkip);
        }

        public void RefreshAo(string currency)
        {
            if (selectedCurrency != currency)
            {
                return;
            }
            new BarGraph(graph, "Awesome Oscillator", currentAo, Colors.Yellow, Colors.Blue).Draw();
        }
    }
}
