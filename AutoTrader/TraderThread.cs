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

namespace AutoTrader
{
    public class TraderThread
    {
        private const string VERSION = "0.26";
        private const int COLLECTOR_WAIT = 1 * 60 * 1000;
        private const int TRADE_WAIT = 5 * 1000;
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

            var buyBuilder = new StringBuilder();
            var sellBuilder = new StringBuilder();

            BuildBuyTrainData(buyBuilder, sellBuilder);

            File.WriteAllText(Path.Combine(exportPath, "BuyTrainingData.txt"), buyBuilder.ToString());
            File.WriteAllText(Path.Combine(exportPath, "SellTrainingData.txt"), sellBuilder.ToString());

            Logger.Info("Done");
        }

        private void BuildBuyTrainData(StringBuilder buyBuilder, StringBuilder sellBuilder)
        {
            foreach (ITrader trader in Traders)
            {
                try
                {
                    trader.BotManager.Refresh();

                    SimpleMovingAverage smaSlow = new SimpleMovingAverage(trader.BotManager.Prices, 5);
                    SimpleMovingAverage smaFast = new SimpleMovingAverage(trader.BotManager.Prices, 9);

                    SimpleMovingAverageOscillator ao = new SimpleMovingAverageOscillator(trader.BotManager.Prices, 5, 9);
                    RelativeStrengthIndex rsi = new RelativeStrengthIndex(trader.BotManager.Prices, 14);
                    MovingAverageConvergenceDivergence macd = new MovingAverageConvergenceDivergence(trader.BotManager.Prices, 12, 26, 9);

                    ExponentialMovingAverage ema24 = new ExponentialMovingAverage(trader.BotManager.Prices, 24);
                    ExponentialMovingAverage ema48 = new ExponentialMovingAverage(trader.BotManager.Prices, 48);
                    ExponentialMovingAverage ema100 = new ExponentialMovingAverage(trader.BotManager.Prices, 100);

                    StochasticsMomentum sto = new StochasticsMomentum(trader.BotManager.Prices, 14);
                    StochasticsMomentumIndex stoIndex = new StochasticsMomentumIndex(trader.BotManager.Prices, 14, 3, 3);

                    bool[] buys = new bool[trader.BotManager.Prices.Count];
                    bool[] sells = new bool[trader.BotManager.Prices.Count];

                    decimal highPrice = decimal.MinValue;
                    int sellIndex = -1;

                    for (int j = trader.BotManager.Prices.Count - 2; j > 0; j--)
                    {
                        decimal prevPrice = trader.BotManager.Prices[j - 1].Close;
                        decimal currentPrice = trader.BotManager.Prices[j].Close;
                        decimal nextPrice = trader.BotManager.Prices[j + 1].Close;

                        if (highPrice < currentPrice)
                        {
                            highPrice = currentPrice;
                            sellIndex = j;
                        }
                        else
                        {
                            if (nextPrice > currentPrice &&  prevPrice > currentPrice && highPrice > currentPrice * 1.05M)
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
                    foreach (var price in trader.BotManager.Prices)
                    {
                        decimal?[] values = new decimal?[] { 
                                price.Open / price.Close, price.Low / price.Close, price.Low / price.High, price.High / price.Close, 
                                smaSlow[i].Tick, smaFast[i].Tick, ao[i].Tick,
                                rsi[i].Tick, 
                                macd[i].Tick.MacdLine, macd[i].Tick.SignalLine, macd[i].Tick.MacdHistogram, ema24[i].Tick,
                                ema48[i].Tick, ema100[i].Tick };

                        Append(buyBuilder, values);
                        Append(sellBuilder, values);

                        buyBuilder.AppendLine(buys[i] ? "1" : "0");
                        sellBuilder.AppendLine(sells[i] ? "1" : "0");

                        i++;
                    }

                    Logger.Info($"{trader.TargetCurrency} : Training data written to disk.");
                }
                catch (Exception ex)
                {
                    Logger.Err($"Error in trader: {trader.TraderId}, ex: {ex.Message} {ex.StackTrace ?? string.Empty}");
                }
            }
        }

        private static StringBuilder Append(StringBuilder builder, decimal?[] values)
        {
            foreach (var value in values)
            {
                builder.Append(value.HasValue ? value.Value.ToString("N9", CultureInfo.InvariantCulture) : "N/A").Append(";");
            }
            return builder;
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
