using System;
using Trady.Analysis;
using Trady.Analysis.Extension;
using Trady.Analysis.Indicator;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class MacdBot : TradingBotBase, ITradingBot
    {
        public override string Name => nameof(MacdBot);
        public override Predicate<IIndexedOhlcv> BuyRule =>
            Rule.Create(c => c.Index > 10).
            And(c => c.IsSmaBearish(5) && c.IsSmaBullish(24) && c.IsAboveEma(96)).
            And(c => c.Get<MovingAverageConvergenceDivergence>(12, 26, 9)[c.Index].Tick.SignalLine > c.Get<MovingAverageConvergenceDivergence>(12, 26, 9)[c.Index].Tick.MacdLine).
            And(c => c.Get<RelativeStrengthIndex>(12, 26, 9)[c.Index].Tick.Value >= c.Get<RelativeStrengthIndex>(12, 26, 9)[c.Index - 1].Tick.Value);
        public override Predicate<IIndexedOhlcv> SellRule =>
            Rule.Create(c => c.IsBullish() && c.IsEmaBullish(48) && c.IsEmaBullish(96) && c.IsSmaBullish(6)).
            And(c => c.IsAboveEma(24) && c.IsAboveEma(96)).
            And(c => c.Get<RateOfChange>(24)[c.Index].Tick > MinRateOfChange);

        public MacdBot(TradingBotManager botManager) : base(botManager, TradePeriod.Long)
        {
            this.botManager = botManager;
        }
    }
}
