using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutoTrader.Config;
using AutoTrader.Db;
using AutoTrader.Traders;
using AutoTrader.Api;
using AutoTrader.Log;
using System.IO;
using System.Text;
using System.Globalization;
using AutoTrader.Db.Entities;

namespace AutoTrader
{
    public class TraderThread
    {
        private const string VERSION = "0.25";
        private const int COLLECTOR_WAIT = 1 * 60 * 1000;
        private const int TRADE_WAIT = 5 * 1000;
        private const int BUYER_NUMBER = 12;
        private const string FIAT = "HUF";
        private const bool collectPrices = false;

        protected static Store Store => Store.Instance;

        private ITradeLogger Logger = TradeLogManager.GetLogger("AutoTrader");
        public static List<ITrader> Traders { get; } = new List<ITrader>();

        public ITrader GetTrader(string targetCurrency)
        {
            return Traders.FirstOrDefault(t => t.TargetCurrency == targetCurrency);
        }

        public Thread GetTraderThread(string appName = null)
        {
            if (appName != null)
            {
                Logger = TradeLogManager.GetLogger(appName);
            }
            return new Thread(Trade);
        }

        public Thread GetCollectorThread(string appName = null)
        {
            if (appName != null)
            {
                Logger = TradeLogManager.GetLogger(appName);
            }
            return new Thread(Collect);
        }

        public void Trade()
        {
            NiceHashApi niceHashApi = GetNiceHashApi();
            Logger.Info("NiceHash AutoTrader " + VERSION);

            niceHashApi.QueryServerTime();
            Logger.Info("Server time:" + niceHashApi.ServerTime);

            CreateTraders(niceHashApi);

            bool first = true;

            do
            {
                int i = 0;
                foreach (ITrader trader in Traders.OrderByDescending(t => t.Order).ToList())
                {
                    try
                    {
                        trader.Trade(i < BUYER_NUMBER && !first);
                        i++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Err($"Error in trader: {trader.TraderId}, ex: {ex.Message} {ex.StackTrace ?? string.Empty}");
                    }
                }
                first = false;
                Logger.Info($"Waiting {TRADE_WAIT / 1000} seconds...");
                Thread.Sleep(TRADE_WAIT);
            } while (true);
        }

        public void Collect()
        {
            NiceHashApi niceHashApi = GetNiceHashApi();
            Logger.Info("NiceHash AutoTrader Collector " + VERSION);

            niceHashApi.QueryServerTime();
            Logger.Info("Server time:" + niceHashApi.ServerTime);

            CreateTraders(niceHashApi);

            Tuple<double, double> previousTotalBalance = new Tuple<double, double>(double.MinValue, double.MinValue);

            do
            {
                var totalBalance = GetTotalFiatBalance(niceHashApi);
                if (totalBalance.Item1 != previousTotalBalance.Item1 || totalBalance.Item2 != previousTotalBalance.Item2)
                {
                    Logger.Info($"Balance changed: {totalBalance.Item1:N8} BTC => {totalBalance.Item2:N1} HUF");
                    Store.Instance.TotalBalances.Save(new TotalBalance { BtcBalance = totalBalance.Item1, FiatBalance = totalBalance.Item2, Date = DateTime.Now });
                    previousTotalBalance = totalBalance;
                }

                if (collectPrices)
                {
                    foreach (ITrader trader in Traders)
                    {
                        try
                        {
                            trader.GetAndStoreCurrentOrders();
                        }
                        catch (Exception ex)
                        {
                            Logger.Err($"Error in trader: {trader.TraderId}, ex: {ex.Message} {ex.StackTrace ?? string.Empty}");
                        }
                    }
                }
                Logger.Info($"Waiting {COLLECTOR_WAIT / 1000} seconds...");
                Thread.Sleep(COLLECTOR_WAIT);
            } while (true);

        }

        public void ExportTraindData(string exportPath)
        {
            NiceHashApi niceHashApi = GetNiceHashApi();
            Logger.Info("NiceHash AutoTrader Train data exporter " + VERSION);

            niceHashApi.QueryServerTime();
            Logger.Info("Server time:" + niceHashApi.ServerTime);

            CreateTraders(niceHashApi);

            var stringBuilder = new StringBuilder();
            foreach (ITrader trader in Traders)
            {
                try
                {
                    Logger.Info($"Exporting {trader.TargetCurrency} past prices...");
                    var r = trader.GetAllPastPrices().OrderBy(p => p.Time);
                    
                    foreach (Price price in r)
                    {
                        stringBuilder.Append(price.Time.Ticks).Append(";");
                        stringBuilder.AppendLine(price.Value.ToString("N8", CultureInfo.InvariantCulture));
                    }

                    Logger.Info($"{r.Count()} prices written to disk.");
                }
                catch (Exception ex)
                {
                    Logger.Err($"Error in trader: {trader.TraderId}, ex: {ex.Message} {ex.StackTrace ?? string.Empty}");
                }
                break;
            }

            File.WriteAllText(Path.Combine(exportPath,"AiData.txt"), stringBuilder.ToString());

            Logger.Info("Done");
        }

        private static Tuple<double, double> GetTotalFiatBalance(NiceHashApi niceHashApi)
        {
            Api.Objects.TotalBalance totalBalance = niceHashApi.GetTotalBalance(FIAT);
            var btcCurrency = totalBalance.currencies.FirstOrDefault(c => c.currency == BtcTrader.BTC);
            if (totalBalance?.total != null && btcCurrency != null)
            {
                return new Tuple<double, double>(totalBalance.total.totalBalance, totalBalance.total.totalBalance * btcCurrency.fiatRate);
            }
            return new Tuple<double, double>(0, 0);
        }

        private static void CreateTraders(NiceHashApi niceHashApi)
        {
            Traders.Clear();

            Symbols symbols = niceHashApi.GetExchangeSettings();

            foreach (Symbol symbol in symbols.symbols.Where(s => s.baseAsset != BtcTrader.BTC))
            {
                if (!Traders.Any(t => t.TargetCurrency == symbol.baseAsset))
                {
                    Traders.Add(new BtcTrader(symbol.baseAsset));
                }
            }
        }

        private static NiceHashApi GetNiceHashApi()
        {
            Store.Connect();
            return NiceHashApi.Create(ProdConfig.Instance);
        }
    }
}
