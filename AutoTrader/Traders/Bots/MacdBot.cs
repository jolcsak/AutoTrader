using System;
using Trady.Analysis;
using Trady.Analysis.Extension;
using Trady.Analysis.Indicator;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class MacdBot : TradingBotBase, ITradingBot
    {
        public const int EMA_PERIOD = 48;

        public override string Name => nameof(MacdBot);
        public override Predicate<IIndexedOhlcv> BuyRule => Rule.Create(c => c.IsEmaBearish(48) && c.IsEmaBearish(96)).
            And(c => c.IsAboveEma(24) && c.IsBelowEma(48) && c.IsBelowEma(96)).
            And(c => c.Get<RateOfChange>(4)[c.Index].Tick > MinRateOfChange);
        public override Predicate<IIndexedOhlcv> SellRule => Rule.Create(c => c.IsEmaBullish(48) && c.IsEmaBullish(96)).And(c => c.IsAboveEma(24) && c.IsAboveEma(96)).And(c => c.Get<RateOfChange>(4)[c.Index].Tick > MinRateOfChange);

        //public override Predicate<IIndexedOhlcv> BuyRule => Rule.Create(c => c.IsEmaBullish(24) && c.IsEmaBullish(48) && c.IsEmaBullish(96)).And(c => c.IsAboveEma(200)).And(c => c.IsBullish() && c.Get<RelativeStrengthIndex>(14)[c.Index].Tick.IsTrue(t => t > 50));
        //public override Predicate<IIndexedOhlcv> SellRule => Rule.Create(c => c.IsEmaBearish(24) && c.IsEmaBearish(48) && c.IsEmaBearish(96)).And(c => c.IsBelowEma(200)).And(c => c.IsBearish() && c.Get<RelativeStrengthIndex>(14)[c.Index].Tick.IsTrue(t => t < 50));

        public MacdBot(TradingBotManager botManager) : base(botManager, TradePeriod.Long)
        {
            this.botManager = botManager;
        }
    }
}
