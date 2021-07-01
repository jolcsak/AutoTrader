using System;
using System.Linq;
using System.Threading;
using AutoTrader.Traders;
using AutoTrader.Api;
using AutoTrader.Log;

namespace AutoTrader
{
    public class TraderThread : TraderCollection
    {
        private const int TRADE_WAIT = 100;

        public Thread GetTraderThread(string appName = null)
        {
            if (appName != null)
            {
                Logger = TradeLogManager.GetLogger(appName);
            }
            return new Thread(Trade);
        }

        public void Trade()
        {
            NiceHashApi niceHashApi = GetNiceHashApi();
            Logger.Info("NiceHash AutoTrader " + VERSION);

            niceHashApi.QueryServerTime();
            Logger.Info("Server time:" + niceHashApi.ServerTime);

            CreateTraders(niceHashApi);

            foreach (ITrader trader in Traders)
            {
                trader.Init();
            }

            bool first = true;
            do
            {
                NiceHashTraderBase.FiatRate = TradingBotManager.GetTotalFiatBalance().Item2;
                TradingBotManager.RefreshBalanceHistory();

                foreach (ITrader trader in Traders.OrderByDescending(t => t.Order).ToList())
                {
                    try
                    {
                        trader.Trade(trader.Order >= 10 && !first);
                    }
                    catch (Exception ex)
                    {
                        Logger.Err($"Error in trader: {trader.TraderId}, ex: {ex.Message} {ex.StackTrace ?? string.Empty}");
                    }
                }
                first = false;
                Thread.Sleep(TRADE_WAIT);
            } while (true);
        }
    }
}
