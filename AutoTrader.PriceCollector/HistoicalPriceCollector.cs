using System;
using System.Threading;
using AutoTrader.Api;
using AutoTrader.Db.Entities;
using AutoTrader.Db;
using AutoTrader.Traders;
using AutoTrader.Log;

namespace AutoTrader.PriceCollector
{
    public class HistoicalPriceCollector : TraderCollection
    {
        private const int COLLECTOR_WAIT = 1 * 60 * 1000;
        private const bool CollectPrices = false;

        public Thread GetCollectorThread(string appName = null)
        {
            if (appName != null)
            {
                Logger = TradeLogManager.GetLogger(appName);
            }
            return new Thread(Collect);
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
                    Logger.Info($"Balance changed: {totalBalance.Item1:N8} {NiceHashApi.BTC} => {totalBalance.Item2:N1} HUF");
                    Store.Instance.TotalBalances.Save(new TotalBalance { BtcBalance = totalBalance.Item1, FiatBalance = totalBalance.Item1 * totalBalance.Item2, Date = DateTime.Now });
                    previousTotalBalance = totalBalance;
                }

                if (CollectPrices)
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
    }
}
