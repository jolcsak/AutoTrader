using AutoTrader.Log;
using AutoTrader.Traders.Trady;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Trady.Analysis.Indicator;
using Trady.Core.Infrastructure;

namespace AutoTrader.MachineLearning
{
    public class HistoricalPriceExporter : TraderCollection
    {

        private static CultureInfo cultureInfo = CultureInfo.InvariantCulture;

        private ITradeLogger Logger = TradeLogManager.GetLogger("HistoricalPriceExporter");

        public void ExportTraindData(string exportPath, int count)
        {
            //NiceHashApi niceHashApi = GetNiceHashApi();
            //Logger.Info("NiceHash AutoTrader Train data exporter " + VERSION);

            //niceHashApi.QueryServerTime();
            //Logger.Info("Server time:" + niceHashApi.ServerTime);

            // CreateTraders(niceHashApi);

            BuildTrainData(exportPath, count);

            Logger.Info("Done");
        }

        private void BuildTrainData(string exportPath, int count)
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

            for (int i = 0; i < count; i++)
            {
                var buyBuilder = new StringBuilder();
                var sellBuilder = new StringBuilder();

                Logger.Info($"Writing FAKE #{i}/{count}...");
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
            SimpleMovingAverage smaSlow = new SimpleMovingAverage(Prices, 64);
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

            for (int j = Prices.Count - 3; j > 1; j--)
            {
                decimal prevPrice = Prices[j - 1].Close;
                decimal prevPrevPrice = Prices[j - 2].Close;
                decimal currentPrice = Prices[j].Close;
                decimal nextPrice = Prices[j + 1].Close;

                if (highPrice < currentPrice)
                {
                    highPrice = currentPrice;
                    sellIndex = j;
                }
                else
                {
                    if (nextPrice > currentPrice && prevPrice > currentPrice && prevPrevPrice > currentPrice && highPrice > currentPrice * 1.1M)
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
                                stoIndex[i].Tick / 100
                                };

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
                builder.Append(value.HasValue ? value.Value.ToString("N20", cultureInfo) : "N/A").Append(";");
            }
            return builder;
        }
    }
}
