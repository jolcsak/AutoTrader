using System;
using System.Linq;
using System.Threading;
using AutoTrader.Traders;
using AutoTrader.Api;
using AutoTrader.Log;
using AutoTrader.Traders.Bots;
using Newtonsoft.Json;
using AutoTrader.Util;

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
            NiceHashApi niceHashApi = GetNiceHashApi();
            Logger.Info($"NiceHash AutoTrader {VERSION}");

            niceHashApi.QueryServerTime();
            Logger.Info("Server time:" + niceHashApi.ServerTime);

            CreateTraders(niceHashApi, shouldInit: true);

            bool first = true;
            do
            {
                try
                {
                    bool isBenchMarking = TradingBotManager.IsBenchmarking;

                    if (isBenchMarking)
                    {
                        if (TradingBotManager.BenchmarkIteration % 2 == 0)
                        {
                            BenchmarkBot.GenerateRules(new BenchmarkData());
                        }
                        else
                        {
                            BenchmarkBot.SwapRules();
                        }
                        TradingBotManager.BenchmarkIteration++;
                    }

                    if (!isBenchMarking)
                    {
                        NiceHashTraderBase.FiatRate = TradingBotManager.GetTotalFiatBalance().Item2;
                        TradingBotManager.RefreshBalanceHistory();
                    }

                    Traders.OrderByDescending(t => t.Order).ForAll(t => Trade(first, t), isParallel: true);

                    double sumProfit = Traders.Where(t => t.Order > 0).Sum(t => t.Order);

                    if (isBenchMarking && TradingBotManager.IsBenchmarking)
                    {
                        var benchmarkData = Store.BenchmarkDataList.GetBenchmarkData();
                        if (sumProfit > BenchmarkBot.MaxBenchProfit)
                        {
                            BenchmarkBot.MaxBenchProfit = sumProfit;
                            BenchmarkBot.MaxBenchProfitData = BenchmarkBot.Data;
                            if (benchmarkData.Profit < sumProfit)
                            {
                                benchmarkData.Profit = sumProfit;
                                benchmarkData.Data = JsonConvert.SerializeObject(BenchmarkBot.MaxBenchProfitData);
                                Store.BenchmarkDataList.SaveOrUpdate(benchmarkData);
                            }
                        }
                        Logger.LogBenchmarkIteration(TradingBotManager.BenchmarkIteration, BenchmarkBot.MaxBenchProfit, benchmarkData.Profit);
                    }
                    else
                    {
                        Logger.LogBenchmarkIteration(0, sumProfit, sumProfit);
                    }

                    first = false;
                    Thread.Sleep(TRADE_WAIT);
                } catch (Exception ex)
                {
                    Logger.Err(ex.Message + " " + ex.StackTrace);
                }
            } while (true);
        }

        private void Trade(bool first, ITrader trader)
        {
            try
            {
                trader.Trade((TradingBotManager.IsBenchmarking || trader.Order > 0) && !first);
            }
            catch (Exception ex)
            {
                Logger.Err($"Error in trader: {trader.TraderId}, ex: {ex.Message} {ex.StackTrace ?? string.Empty}");
            }
        }
    }
}
