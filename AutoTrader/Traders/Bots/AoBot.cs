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

        public override Predicate<IIndexedOhlcv> SellRule =>
                        Rule.Create(c => c.Index > 0).
                        And(c => c.Get<ExponentialMovingAverage>(5)[c.Index].Tick > c.Get<ExponentialMovingAverage>(20)[c.Index].Tick).
                        And(c => c.Get<ExponentialMovingAverage>(5)[c.Index].Tick > c.Get<SimpleMovingAverage>(20)[c.Index].Tick);

        public override Predicate<IIndexedOhlcv> BuyRule =>
                Rule.Create(c => c.Index > 0).
                And(c => c.Get<ExponentialMovingAverage>(5)[c.Index].Tick < c.Get<ExponentialMovingAverage>(20)[c.Index].Tick).
                And(c => c.Get<ExponentialMovingAverage>(5)[c.Index].Tick < c.Get<SimpleMovingAverage>(20)[c.Index].Tick).
                And(c => c.IsMacdBearishCross()).
                And(c => c.IsRsiOversold(4)).
                And(c => c.IsEmaBullish(24));

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
