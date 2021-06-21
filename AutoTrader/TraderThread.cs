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
using Trady.Analysis.Indicator;
using Trady.Core.Infrastructure;
using AutoTrader.Traders.Trady;

namespace AutoTrader
{
    public class TraderThread
    {
        private const string VERSION = "0.3";

        private const int FAKE_CYCLE = 1000;

        private const int COLLECTOR_WAIT = 1 * 60 * 1000;
        private const int TRADE_WAIT = 5 * 1000;
        private const string FIAT = "HUF";
        private const bool collectPrices = false;

        // CultureInfo.InvariantCulture
        private static CultureInfo cultureInfo = CultureInfo.InvariantCulture;

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
                NiceHashTraderBase.FiatRate = TradingBotManager.GetTotalFiatBalance().Item2;

                foreach (ITrader trader in Traders.OrderByDescending(t => t.Order).ToList())
                {
                    try
                    {
                        trader.Trade(trader.Order > 0 && !first);
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
                    Store.Instance.TotalBalances.Save(new TotalBalance { BtcBalance = totalBalance.Item1, FiatBalance = totalBalance.Item1 * totalBalance.Item2, Date = DateTime.Now });
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
            //NiceHashApi niceHashApi = GetNiceHashApi();
            //Logger.Info("NiceHash AutoTrader Train data exporter " + VERSION);

            //niceHashApi.QueryServerTime();
            //Logger.Info("Server time:" + niceHashApi.ServerTime);

            // CreateTraders(niceHashApi);

            BuildTrainData(exportPath);

            Logger.Info("Done");
        }

        private void BuildTrainData(string exportPath)
        {
            string buyDataPath = Path.Combine(exportPath, "BuyTrainingData.txt");
            if (File.Exists(buyDataPath))
            {
                File.Delete(buyDataPath);
            }

            string sellDataPath = Path.Combine(exportPath, "SellTrainingData.txt");
            if (File.Exists(sellDataPath))
            {
                File.Delete(sellDataPath);
            }

            for (int i = 0; i < FAKE_CYCLE; i++)
            {
                var buyBuilder = new StringBuilder();
                var sellBuilder = new StringBuilder();

                Logger.Info($"Writing FAKE #{i}/{FAKE_CYCLE}...");
                var prices = new FakeNiceHashImporter().Import(string.Empty, DateTime.Now.AddMonths(-1), DateTime.Now);
                CollectPrices(buyBuilder, sellBuilder, prices);

                using (StreamWriter sw = File.AppendText(buyDataPath))
                {
                    sw.Write(buyBuilder);
                }

                using (StreamWriter sw = File.AppendText(sellDataPath))
                {
                    sw.Write(sellBuilder);
                }
            }

            //foreach (ITrader trader in Traders)
            //{
            //    try
            //    {
            //        trader.BotManager.Refresh();
            //        Logger.Info($"Writing {trader.BotManager.Trader.TargetCurrency} ...");
            //        CollectPrices(buyBuilder, sellBuilder, trader.BotManager.Prices);
            //    }
            //    catch (Exception ex)
            //    {
            //        Logger.Err($"Error in trader: {trader.TraderId}, ex: {ex.Message} {ex.StackTrace ?? string.Empty}");
            //    }
            //}
        }

        private static void CollectPrices(StringBuilder buyBuilder, StringBuilder sellBuilder, IList<IOhlcv> Prices)
        {
            SimpleMovingAverage smaSlow = new SimpleMovingAverage(Prices, 5);
            SimpleMovingAverage smaFast = new SimpleMovingAverage(Prices, 9);

            SimpleMovingAverageOscillator ao = new SimpleMovingAverageOscillator(Prices, 5, 9);
            RelativeStrengthIndex rsi = new RelativeStrengthIndex(Prices, 14);
            MovingAverageConvergenceDivergence macd = new MovingAverageConvergenceDivergence(Prices, 12, 26, 9);

            ExponentialMovingAverage ema24 = new ExponentialMovingAverage(Prices, 24);
            ExponentialMovingAverage ema48 = new ExponentialMovingAverage(Prices, 48);
            ExponentialMovingAverage ema100 = new ExponentialMovingAverage(Prices, 100);

            StochasticsMomentum sto = new StochasticsMomentum(Prices, 14);
            StochasticsMomentumIndex stoIndex = new StochasticsMomentumIndex(Prices, 14, 3, 3);

            bool[] buys = new bool[Prices.Count];
            bool[] sells = new bool[Prices.Count];

            decimal highPrice = decimal.MinValue;
            int sellIndex = -1;

            for (int j = Prices.Count - 2; j > 0; j--)
            {
                decimal prevPrice = Prices[j - 1].Close;
                decimal currentPrice = Prices[j].Close;
                decimal nextPrice = Prices[j + 1].Close;

                if (highPrice < currentPrice)
                {
                    highPrice = currentPrice;
                    sellIndex = j;
                }
                else
                {
                    if (nextPrice > currentPrice && prevPrice > currentPrice && highPrice > currentPrice * 1.05M)
                    {
                        buys[j] = true;
                        if (sellIndex > -1)
                        {
                            sells[sellIndex] = true;
                        }
                        highPrice = currentPrice;
                        sellIndex = j;
                    }
                }
            }

            int i = 0;
            foreach (var price in Prices)
            {
                decimal?[] values = new decimal?[] {
                                price.Open / price.Close, 
                                price.Low / price.Close, 
                                price.Low / price.High, 
                                price.High / price.Close,
                                smaSlow[i].Tick / price.Close,
                                smaFast[i].Tick / price.Close,
                                rsi[i].Tick / 100,
                                ema24[i].Tick / price.Close,
                                ema48[i].Tick / price.Close, 
                                ema100[i].Tick / price.Close,
                                stoIndex[i].Tick / 100};

                //decimal?[] values = new decimal?[] { price.Close };

                if (!values.Any(v => v == null))
                {
                    Append(buyBuilder, values);
                    Append(sellBuilder, values);

                    buyBuilder.AppendLine(buys[i] ? "1" : "0");
                    sellBuilder.AppendLine(sells[i] ? "1" : "0");
                }

                i++;
            }
        }

        private static StringBuilder Append(StringBuilder builder, decimal?[] values)
        {
            foreach (var value in values)
            {
                builder.Append(value.HasValue ? value.Value.ToString("N9", cultureInfo) : "N/A").Append(";");
            }
            return builder;
        }

        private static Tuple<double, double> GetTotalFiatBalance(NiceHashApi niceHashApi)
        {
            Api.Objects.TotalBalance totalBalance = niceHashApi.GetTotalBalance(FIAT);
            var btcCurrency = totalBalance.currencies.FirstOrDefault(c => c.currency == BtcTrader.BTC);
            if (totalBalance?.total != null && btcCurrency != null)
            {
                return new Tuple<double, double>(totalBalance.total.totalBalance, btcCurrency.fiatRate);
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
