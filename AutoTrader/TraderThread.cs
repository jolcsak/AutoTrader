using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutoTrader.Config;
using AutoTrader.Db;
using AutoTrader.Traders;
using AutoTrader.Api;
using AutoTrader.Log;
using AutoTrader.Api.Objects;
using System.Reflection;
using System.IO;
using System.Text;
using System.Globalization;
using AutoTrader.Db.Entities;

namespace AutoTrader
{
    public class TraderThread
    {
        private const string VERSION = "0.24";

        private const int COLLECTOR_WAIT = 1 * 60 * 1000;
        private const int TRADE_WAIT = 20 * 1000;
        private const int BUYER_NUMBER = 12;

        private const string FIAT = "HUF";

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

            do
            {
                int i = 0;
                foreach (ITrader trader in Traders.OrderByDescending(t => t.Order).ToList())
                {
                    try
                    {
                        trader.Trade(i < BUYER_NUMBER);
                        i++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Err($"Error in trader: {trader.TraderId}, ex: {ex.Message} {ex.StackTrace ?? string.Empty}");
                    }
                }
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

            double previousTotalFiatBalance = double.MinValue;

            do
            {
                double totalFiatBalance = GetTotalFiatBalance(niceHashApi);
                if (totalFiatBalance != previousTotalFiatBalance)
                {
                    Store.Instance.TotalBalances.Save(new Db.Entities.TotalBalance { Balance = totalFiatBalance, Date = DateTime.Now });
                    previousTotalFiatBalance = totalFiatBalance;
                }

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
            
            foreach (ITrader trader in Traders)
            {
                try
                {
                    Logger.Info($"Exporting {trader.TargetCurrency} past prices...");
                    var r = trader.GetAllPastPrices().OrderBy(p => p.Time);
                    var stringBuilder = new StringBuilder();
                    foreach (Price price in r)
                    {
                        stringBuilder.Append(price.Time.Ticks).Append(";");
                        stringBuilder.AppendLine(price.Value.ToString("N8", CultureInfo.InvariantCulture));
                    }

                    File.WriteAllText(Path.Combine(exportPath, trader.TargetCurrency + ".txt"), stringBuilder.ToString());
                    Logger.Info($"{r.Count()} prices written to disk.");
                }
                catch (Exception ex)
                {
                    Logger.Err($"Error in trader: {trader.TraderId}, ex: {ex.Message} {ex.StackTrace ?? string.Empty}");
                }
            }
            Logger.Info("Done");
        }

        private static double GetTotalFiatBalance(NiceHashApi niceHashApi)
        {
            Api.Objects.TotalBalance totalBalance = niceHashApi.GetTotalBalance(FIAT);
            var btcCurrency = totalBalance.currencies.FirstOrDefault(c => c.currency == BtcTrader.BTC);
            return (totalBalance?.total != null && btcCurrency != null)? totalBalance.total.totalBalance * btcCurrency.fiatRate : 0;
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
