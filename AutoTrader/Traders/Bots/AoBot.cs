using System;
using Trady.Analysis;
using Trady.Analysis.Extension;
using Trady.Analysis.Indicator;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class AoBot : TradingBotBase, ITradingBot
    {
        public const int EMA_PERIOD = 48;
        public const int RSI_PERIOD = 14;

        private const int COOLDOWN_IN_MINUTES = 60;
        private const int PRICE_PERCENTAGE_CHANGE = 5;

        public override string Name => nameof(AoBot);


        public override Predicate<IIndexedOhlcv> BuyRule =>
                        Rule.Create(c => c.Index > 0 && (c.IsBreakingLowestClose(32) || c.IsBreakingHistoricalHighestClose())).
                        And(c => c.IsRsiOversold(RSI_PERIOD)).
                        And(c => c.Get<RateOfChange>(4)[c.Index].Tick > MinRateOfChange);


        public override Predicate<IIndexedOhlcv> SellRule =>
            Rule.Create(c => c.Index > 0 && (c.IsBreakingHighestClose(32) || c.IsBreakingHistoricalLowestClose())).
            And(c => c.IsRsiOverbought(RSI_PERIOD)).And(c => c.Get<RateOfChange>(4)[c.Index].Tick > MinRateOfChange);


        //public override Predicate<IIndexedOhlcv> BuyRule =>
        //                Rule.Create(c => c.Get<StochasticsMomentumIndex>(3, 3, 14)[c.Index].Tick.IsTrue(t => t > 50)).
        //                And(c => c.Get<RelativeStrengthIndex>(RSI_PERIOD)[c.Index].Tick.IsTrue(t => t > 50)).
        //                And(c => c.Get<MovingAverageConvergenceDivergence>(12, 26, 9)[c.Index].Tick.MacdLine > c.Get<MovingAverageConvergenceDivergence>(12, 26, 9)[c.Index].Tick.SignalLine).
        //                And(c => !c.IsBreakingHighestClose(24)).
        //                And(c => !c.IsRsiOversold());   


        //public override Predicate<IIndexedOhlcv> SellRule =>
        //    Rule.Create(c => c.Get<MovingAverageConvergenceDivergence>(12, 26, 9)[c.Index].Tick.MacdLine < c.Get<MovingAverageConvergenceDivergence>(12, 26, 9)[c.Index].Tick.SignalLine).
        //    And(c => !c.IsBreakingLowestClose(24)).
        //    And(c => !c.IsRsiOverbought()).
        //    Or(c => c.IsBreakingHighestClose(72));

        static AoBot()
        {
            //RuleRegistry.Register("isAbove50RSI", (ic, p) => ic.Get<RelativeStrengthIndex>(p[0])[ic.Index].Tick.IsTrue(t => t > 50));
        }

        public AoBot(TradingBotManager botManager) : base(botManager, TradePeriod.Short, COOLDOWN_IN_MINUTES)
        {
            this.botManager = botManager;
        }
    }
}
