using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutoTrader.Config;
using AutoTrader.Db;
using AutoTrader.Traders;
using AutoTrader.Api;
using AutoTrader.Log;

namespace AutoTrader
{
    public class TraderThread
    {
        private const string VERSION = "0.2";

        private const int COLLECTOR_WAIT = 3 * 60 * 1000;
        private const int TRADE_WAIT = 2 * 60 * 1000;
        private const int COLLECTOR_TRADER_DELAY_TIME = 1000;
        private const int APP_TRADER_DELAY_TIME = 10;
        private const int BUYER_NUMBER = 12;

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

            IDictionary<string, double> myBalances = niceHashApi.GetBalances();
            foreach (string currency in myBalances.Keys)
            {
                Logger.Info($"Current balance in {currency}: {myBalances[currency]}");
            }

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
                        Thread.Sleep(APP_TRADER_DELAY_TIME);
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

            do
            {
                foreach (ITrader trader in Traders)
                {
                    try
                    {
                        trader.GetAndStoreCurrentOrders();
                        Thread.Sleep(COLLECTOR_TRADER_DELAY_TIME);
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
