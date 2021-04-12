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
        private const int WAIT = 5 * 60 * 1000;
        private const int DELAY_TIME = 333;
        private ITradeLogger Logger = TradeLogManager.GetLogger("AutoTrader");

        private static List<ITrader> traders = new List<ITrader>();

        public ITrader GetTrader(string targetCurrency)
        {
            return traders.FirstOrDefault(t => t.TargetCurrency == targetCurrency);
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
            Logger.Info("NiceHash AutoTrader 0.1");

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
                foreach (ITrader trader in traders)
                {
                    try
                    {
                        trader.Trade();
                        Thread.Sleep(DELAY_TIME);
                    }
                    catch (Exception ex)
                    {
                        Logger.Err($"Error in trader: {trader.TraderId}, ex: {ex.Message} {ex.StackTrace ?? string.Empty}");
                    }
                }
                Thread.Sleep(WAIT);
                Logger.Info($"Waiting {WAIT / 1000} seconds...");
            } while (true);
        }

        public void Collect()
        {
            NiceHashApi niceHashApi = GetNiceHashApi();
            Logger.Info("NiceHash AutoTrader Collector 0.1");

            niceHashApi.QueryServerTime();
            Logger.Info("Server time:" + niceHashApi.ServerTime);

            CreateTraders(niceHashApi);

            do
            {
                foreach (ITrader trader in traders)
                {
                    try
                    {
                        trader.GetandStoreCurrentOrders();
                        Thread.Sleep(DELAY_TIME);
                    }
                    catch (Exception ex)
                    {
                        Logger.Err($"Error in trader: {trader.TraderId}, ex: {ex.Message} {ex.StackTrace ?? string.Empty}");
                    }
                }
                Logger.Info($"Waiting {WAIT / 1000} seconds...");
                Thread.Sleep(WAIT);
            } while (true);

        }

        private static void CreateTraders(NiceHashApi niceHashApi)
        {
            traders.Clear();

            Symbols symbols = niceHashApi.GetExchangeSettings();

            foreach (Symbol symbol in symbols.symbols.Where(s => s.baseAsset != BtcTrader.BTC))
            {
                if (!traders.Any(t => t.TargetCurrency == symbol.baseAsset))
                {
                    traders.Add(new BtcTrader(symbol.baseAsset));
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
