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
            Rule.Create(c => c.IsEmaBullish(24) && c.IsEmaBullish(50) && c.IsEmaBullish(100) && c.IsBullish() && c.IsSmaBullish(6) && !c.IsBreakingHighestClose(24) && c.IsSmaOscBullish(6, 24) && c.IsEmaOscBullish(6, 24) && c.IsMacdOscBullish()).
            And(c => c.Get<RateOfChange>(24)[c.Index].Tick > MinRateOfChange);
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
