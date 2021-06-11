using AutoTrader.Ai;
using Microsoft.ML;
using System;
using Trady.Analysis;
using Trady.Analysis.Indicator;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class AiBot : TradingBotBase, ITradingBot
    {

        private const int COOLDOWN_IN_MINUTES = 10;

        private static MLContext mlContext = new MLContext();
        private static PredictionEngine<BuyInput, TradePrediction> buyPredictionEngine;
        private static PredictionEngine<SellInput, TradePrediction> sellPredictionEngine;

        public override Predicate<IIndexedOhlcv> BuyRule => Rule.Create( c => buyPredictionEngine.Predict(GetInput<BuyInput>(c)).Prediction);

        public override Predicate<IIndexedOhlcv> SellRule => Rule.Create(c => sellPredictionEngine.Predict(GetInput<SellInput>(c)).Prediction);

        private T GetInput<T>(IIndexedOhlcv c) where T: TradeInputBase , new()
        {
            var macd = c.Get<MovingAverageConvergenceDivergence>(12, 26, 9)[c.Index];

            return new T
            {
                Currency = botManager.Trader.TargetCurrency.GetHashCode(),
                Open = (float)c.Open,
                Close = (float)c.Close,
                Low = (float)c.Low,
                High = (float)c.High,
                SmaSlow = GetValue(c.Get<SimpleMovingAverage>(5)[c.Index].Tick),
                SmaFast = GetValue(c.Get<SimpleMovingAverage>(9)[c.Index].Tick),
                Ao = GetValue(c.Get<SimpleMovingAverageOscillator>(5, 9)[c.Index].Tick),
                Rsi = GetValue(c.Get<RelativeStrengthIndex>(14)[c.Index].Tick),
                MacdLine = GetValue(macd.Tick.MacdLine),
                MacdSignalLine = GetValue(macd.Tick.SignalLine),
                MacdHistogram = GetValue(macd.Tick.MacdHistogram),
                Ema24 = GetValue(c.Get<ExponentialMovingAverage>(24)[c.Index].Tick),
                Ema48 = GetValue(c.Get<ExponentialMovingAverage>(48)[c.Index].Tick),
                Ema100 = GetValue(c.Get<ExponentialMovingAverage>(100)[c.Index].Tick)
            };
        }

        static AiBot()
        {
            var transformer = mlContext.Model.Load(@"..\..\..\..\AutoTrader.MachineLearning\LearningData\TrainedBuyData.zip", out var modelSchema);
            buyPredictionEngine = mlContext.Model.CreatePredictionEngine<BuyInput, TradePrediction>(transformer);

            transformer = mlContext.Model.Load(@"..\..\..\..\AutoTrader.MachineLearning\LearningData\TrainedSellData.zip", out modelSchema);
            sellPredictionEngine = mlContext.Model.CreatePredictionEngine<SellInput, TradePrediction>(transformer);
        }


        public AiBot(TradingBotManager botManager) : base(botManager, TradePeriod.Short, COOLDOWN_IN_MINUTES)
        {
            this.botManager = botManager;
        }

        private static float GetValue(decimal? value)
        {
            return (float)(value.HasValue ? value.Value : 0);
        }
    }
}
