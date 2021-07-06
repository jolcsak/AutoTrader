using System;
using System.Linq;
using System.Threading;
using AutoTrader.Traders;
using AutoTrader.Api;
using AutoTrader.Log;
using AutoTrader.Traders.Bots;

namespace AutoTrader
{
    public class TraderThread : TraderCollection
    {
        private const int TRADE_WAIT = 100;

        public static ITrader CurrentTrader { get; set; }

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
            try
            {
                NiceHashApi niceHashApi = GetNiceHashApi();
                Logger.Info($"NiceHash AutoTrader {VERSION}");

                niceHashApi.QueryServerTime();
                Logger.Info("Server time:" + niceHashApi.ServerTime);

                CreateTraders(niceHashApi, shouldInit: true);

                bool first = true;
                do
                {
                    bool isBenchMarking = TradingBotManager.IsBenchmarking;

                    if (isBenchMarking)
                    {
                        BenchmarkBot.GenerateRules(new BenchmarkData());
                        TradingBotManager.BenchmarkIteration++;
                    }

                    if (!isBenchMarking)
                    {
                        NiceHashTraderBase.FiatRate = TradingBotManager.GetTotalFiatBalance().Item2;
                        TradingBotManager.RefreshBalanceHistory();
                    }

                    foreach (ITrader trader in Traders.OrderByDescending(t => t.Order).ToList())
                    {
                        try
                        {
                            trader.Trade((TradingBotManager.IsBenchmarking || trader.Order >= 10) && !first);
                        }
                        catch (Exception ex)
                        {
                            Logger.Err($"Error in trader: {trader.TraderId}, ex: {ex.Message} {ex.StackTrace ?? string.Empty}");
                        }
                    }

                    double sumProfit = Traders.Where(t => t.Order > 0).Sum(t => t.Order);

                    if (isBenchMarking && TradingBotManager.IsBenchmarking)
                    {
                        if (sumProfit > BenchmarkBot.MaxBenchProfit)
                        {
                            BenchmarkBot.MaxBenchProfit = sumProfit;
                            BenchmarkBot.MaxBenchProfitData = BenchmarkBot.Data;
                        }
                        Logger.LogBenchmarkIteration(TradingBotManager.BenchmarkIteration, BenchmarkBot.MaxBenchProfit);
                    }
                    else
                    {
                        Logger.LogBenchmarkIteration(0, sumProfit);
                    }

                    first = false;
                    Thread.Sleep(TRADE_WAIT);
                } while (true);
            } catch (Exception ex)
            {
                Logger.Err(ex.Message + " " + ex.StackTrace);
            }
        }
    }
}
