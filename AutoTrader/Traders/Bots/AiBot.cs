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

        public override string Name => "AIBot"; 

        public override Predicate<IIndexedOhlcv> BuyRule => Rule.Create( c => buyPredictionEngine.Predict(GetInput<BuyInput>(c)).Prediction);

        public override Predicate<IIndexedOhlcv> SellRule => Rule.Create(c => sellPredictionEngine.Predict(GetInput<SellInput>(c)).Prediction);

        private T GetInput<T>(IIndexedOhlcv c) where T: TradeInputBase , new()
        {
            //var macd = c.Get<MovingAverageConvergenceDivergence>(12, 26, 9)[c.Index];
            return new T
            {
                Open = (float)(c.Open / c.Close),
                Close = (float)(c.Low / c.Close),
                Low = (float)(c.Low / c.High),
                High = (float)(c.High / c.Close),
                SmaSlow = GetValue(c.Get<SimpleMovingAverage>(64)[c.Index].Tick) / (float)c.Close,
                SmaFast = GetValue(c.Get<SimpleMovingAverage>(9)[c.Index].Tick) / (float)c.Close,
                Rsi = GetValue(c.Get<RelativeStrengthIndex>(14)[c.Index].Tick) / 100,
                Ema24 = GetValue(c.Get<ExponentialMovingAverage>(24)[c.Index].Tick) / (float)c.Close,
                Ema48 = GetValue(c.Get<ExponentialMovingAverage>(48)[c.Index].Tick) / (float)c.Close,
                Ema100 = GetValue(c.Get<ExponentialMovingAverage>(100)[c.Index].Tick) / (float)c.Close,
                StoIndex = GetValue(c.Get<StochasticsMomentumIndex>(14, 3, 3)[c.Index].Tick)
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
