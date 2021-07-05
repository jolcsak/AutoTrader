using System;
using AutoTrader.Db.Entities;
using Trady.Analysis;
using Trady.Analysis.Extension;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class BenchmarkBot : TradingBotBase, ITradingBot
    {
        public static double MaxBenchProfit { get; set; } = double.MinValue;

        public static BenchmarkData MaxBenchProfitData { get; set; }

        public static BenchmarkData Data => data;

        protected static BenchmarkData data;

        protected static Predicate<IIndexedOhlcv>[] subRules;

        protected static Predicate<IIndexedOhlcv> buyRule;

        protected static Predicate<IIndexedOhlcv> sellRule;

        static BenchmarkBot()
        {
            subRules = new Predicate<IIndexedOhlcv>[]
                    {
                        c => c.IsEmaBearish(data.Next(100, "IsEmaBearish")),
                        c => c.IsEmaBullish(data.Next(100, "IsEmaBullish")),
                        c => c.IsEmaBearishCross(data.Next(100, "IsEmaBearishCross_1"), data.Next(100, "IsEmaBearishCross_2")),
                        c => c.IsEmaBullishCross(data.Next(100, "IsEmaBullishCross_1"), data.Next(100, "IsEmaBullishCross_2")),
                        c => c.IsEmaOscBearish(data.Next(100, "IsEmaOscBearish_1"), data.Next(100, "IsEmaOscBearish_2")),
                        c => c.IsEmaOscBullish(data.Next(100, "IsEmaOscBullish_1"), data.Next(100, "IsEmaOscBullish_2")),
                        c => c.IsAboveEma(data.Next(100, "IsAboveEma")),
                        c => c.IsBelowEma(data.Next(100, "IsBelowEma")),
                        c => c.IsAboveSma(data.Next(100, "IsAboveSma")),
                        c => c.IsBelowSma(data.Next(100, "IsBelowSma")),
                        c => c.IsSmaBearish(data.Next(100, "IsSmaBearish")),
                        c => c.IsSmaBullish(data.Next(100, "IsSmaBullish")),
                        c => c.IsSmaBullishCross(data.Next(100, "IsSmaBullishCross_1"), data.Next(100, "IsSmaBullishCross_2")),
                        c => c.IsSmaBearishCross(data.Next(100, "IsSmaBearishCross_1"), data.Next(100, "IsSmaBearishCross_2")),
                        c => c.IsSmaOscBearish(data.Next(100, "IsSmaOscBearish_1"), data.Next(100, "IsSmaOscBearish_2")),
                        c => c.IsSmaOscBullish(data.Next(100, "IsSmaOscBullish_1"), data.Next(100, "IsSmaOscBullish_2")),
                        c => c.ClosePricePercentageChange() > data.Next(100, "ClosePricePercentageChange"),
                        c => c.IsBreakingHighestClose(data.Next(100, "IsBreakingHighestClose")),
                        c => c.IsBreakingHighestHigh(data.Next(100, "IsBreakingHighestHigh")),
                        c => data.Next(c => c.IsBreakingHistoricalLowestClose(), "IsBreakingHistoricalLowestClose")(c),
                        c => data.Next(c => c.IsBreakingHistoricalLowestLow(), "IsBreakingHistoricalLowestLow")(c),
                        c => data.Next(c => c.IsBreakingHistoricalHighestHigh(), "IsBreakingHistoricalHighestHigh")(c),
                        c => data.Next(c => c.IsBreakingHistoricalHighestClose(), "IsBreakingHistoricalHighestClose")(c),
                        c => c.IsBreakingLowestClose(data.Next(100, "IsBreakingLowestClose")),
                        c => c.IsBreakingLowestLow(data.Next(100, "IsBreakingLowestLow")),
                        c => data.Next(c => c.IsBullish(), "IsBullish")(c),
                        c => data.Next(c => c.IsBearish(), "IsBearish")(c),
                        c => c.IsBelowBbLow(data.Next(100, "IsBelowBbLow_1"), data.Next(100, "IsBelowBbLow_2")),
                        c => c.IsAboveBbUp(data.Next(100, "IsAboveBbUp_1"), data.Next(100, "IsAboveBbUp_2")),
                        //c => c.IsFastStoBearishCross(data.Next(100, "IsFastStoBearishCross_1"), data.Next(100, "IsFastStoBearishCross_2")),
                        //c => c.IsFastStoBullishCross(data.Next(100, "IsFastStoBullishCross_1"), data.Next(100, "IsFastStoBullishCross_2")),
                        //c => c.IsFastStoOscBearish(data.Next(100, "IsFastStoOscBearish_1"), data.Next(100, "IsFastStoOscBearish_2")),
                        //c => c.IsFastStoOscBullish(data.Next(100, "IsFastStoOscBullish_1"), data.Next(100, "IsFastStoOscBullish_2")),
                        //c => c.IsFastStoOverbought(data.Next(100, "IsFastStoOverbought_1"), data.Next(100, "IsFastStoOverbought_2")),
//                        c => c.IsFastStoOversold(data.Next(100, "IsFastStoOversold_1"), data.Next(100, "IsFastStoOversold_2")),
                        //c => c.IsSlowStoBearishCross(data.Next(100, "IsSlowStoBearishCross_1"), data.Next(100, "IsSlowStoBearishCross_2")),
                        //c => c.IsSlowStoBullishCross(data.Next(100, "IsSlowStoBullishCross_1"), data.Next(100, "IsSlowStoBullishCross_2")),
                        //c => c.IsSlowStoOscBearish(data.Next(100, "IsSlowStoOscBearish_1"), data.Next(100, "IsSlowStoOscBearish_2")),
                        //c => c.IsSlowStoOscBullish(data.Next(100, "IsSlowStoOscBullish_1"), data.Next(100, "IsSlowStoOscBullish_2")),
                        //c => c.IsSlowStoOverbought(data.Next(100, "IsSlowStoOverbought_1"), data.Next(100, "IsSlowStoOverbought_2")),
                        //c => c.IsSlowStoOversold(data.Next(100, "IsSlowStoOversold_1"), data.Next(100, "IsSlowStoOversold_2")),
                        //c => c.IsFullStoBearishCross(data.Next(100, "IsFullStoBearishCross_1"), data.Next(100, "IsFullStoBearishCross_2"), data.Next(100, "IsFullStoBearishCross_3")),
                        //c => c.IsFullStoBullishCross(data.Next(100, "IsFullStoBullishCross_1"), data.Next(100, "IsFullStoBullishCross_2"), data.Next(100, "IsFullStoBullishCross_3")),
                        //c => c.IsFullStoOscBearish(data.Next(100, "IsFullStoOscBearish_1"), data.Next(100, "IsFullStoOscBearish_2"), data.Next(100, "IsFullStoOscBearish_3")),
                        //c => c.IsFullStoOscBullish(data.Next(100, "IsFullStoOscBullish_1"), data.Next(100, "IsFullStoOscBullish_2"), data.Next(100, "IsFullStoOscBullish_3")),
                        //c => c.IsFullStoOverbought(data.Next(100, "IsFullStoOverbought_1"), data.Next(100, "IsFullStoOverbought_2"), data.Next(100, "IsFullStoOverbought_3")),
                        //c => c.IsFullStoOversold(data.Next(100, "IsFullStoOversold_1"), data.Next(100, "IsFullStoOversold_2"), data.Next(100, "IsFullStoOversold_3")),
                        c => c.IsInBbRange(data.Next(100, "IsInBbRange_1"), data.Next(100, "IsInBbRange_2")),
                        c => c.IsMacdBearishCross(data.Next(100, "IsMacdBearishCross_1"), data.Next(100, "IsMacdBearishCross_2"), data.Next(100, "IsMacdBearishCross_3")),
                        c => c.IsMacdBullishCross(data.Next(100, "IsMacdBullishCross_1"), data.Next(100, "IsMacdBullishCross_2"), data.Next(100, "IsMacdBullishCross_3")),
                        c => c.IsMacdOscBearish(data.Next(100, "IsMacdOscBearish_1"), data.Next(100, "IsMacdOscBearish_2"), data.Next(100, "IsMacdOscBearish_3")),
                        c => c.IsMacdOscBullish(data.Next(100, "IsMacdOscBullish_1"), data.Next(100, "IsMacdOscBullish_2"), data.Next(100, "IsMacdOscBullish_3")),
                        c => data.Next(c => c.IsObvBearish(), "IsObvBearish")(c),
                        c => data.Next(c => c.IsObvBullish(), "IsObvBullish")(c),
                        c => c.IsRsiOverbought(data.Next(100, "IsRsiOverbought")),
                        c => c.IsRsiOversold(data.Next(100, "IsRsiOversold")),
                        c => data.Next(c => c.IsAccumDistBearish(), "IsAccumDistBearish")(c),
                        c => data.Next(c => c.IsAccumDistBullish(), "IsAccumDistBullish")(c)
                    };
        }

        public BenchmarkBot(TradingBotManager botManager) : base(botManager, TradePeriod.Long)
        {
        }

        public static void Clear()
        {
            buyRule = null;
            sellRule = null;
            data = null;
        }

        public static void GenerateRules()
        {
            data = new BenchmarkData();

            int subRuleCount = subRules.Length;

            buyRule = Rule.Create(c => c.Index > 0 && data != null);
            sellRule = Rule.Create(c => c.Index > 0 && data != null);

            int numberOfRules = data.Next(10, "NumberOfRules");
            for (int i = 0; i < numberOfRules; i++)
            {
                buyRule = buyRule.And(subRules[data.Next(subRuleCount - 1, "BuySubRule_" + i)]);
                sellRule = sellRule.And(subRules[data.Next(subRuleCount - 1, "SellSubRule_" + i)]);
            }
        }

        public static void LoadBechmarkRule()
        {
            data = MaxBenchProfitData;

            buyRule = Rule.Create(c => c.Index > 0 && data != null);
            sellRule = Rule.Create(c => c.Index > 0 && data != null);

            if (data != null)
            {
                int numberOfRules = data.Sequence["NumberOfRules"];
                for (int i = 0; i < numberOfRules; i++)
                {
                    buyRule = buyRule.And(subRules[data.Next(numberOfRules - 1, "BuySubRule_" + i)]);
                    sellRule = sellRule.And(subRules[data.Next(numberOfRules - 1, "SellSubRule_" + i)]);
                }
            } 
        }

        public override string Name => nameof(BenchmarkBot);
        public override Predicate<IIndexedOhlcv> BuyRule
        {
            get
            {
                if (buyRule == null)
                {
                    if (TradingBotManager.IsBenchmarking)
                    {
                        GenerateRules();
                    }
                    else
                    {
                        LoadBechmarkRule();
                    }
                }
                return buyRule;
            }
        }
        public override Predicate<IIndexedOhlcv> SellRule
        {
            get
            {
                if (sellRule == null)
                {
                    if (TradingBotManager.IsBenchmarking)
                    {
                        GenerateRules();
                    }
                    else
                    {
                        LoadBechmarkRule();
                    }
                }
                return sellRule;
            }            
        }

        public override SellType ShouldSell(ActualPrice actualPrice, TradeOrder tradeOrder, TradeItem lastTrade) => SellType.None;
    }
}
