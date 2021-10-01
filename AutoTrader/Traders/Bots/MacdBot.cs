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
            And(c => c.Get<RateOfChange>(24)[c.Index].Tick > MinRateOfChange).
            And(c => c.IsEmaBullish(12) && c.IsEmaBullish(48) && c.IsEmaBullish(96)).
            And(c => 
            ((c.Prev.IsMacdOscBearish(12, 26, 9) ||
            c.Prev.Prev.IsMacdOscBearish(12, 26, 9) ||
            c.Prev.Prev.Prev.IsMacdOscBearish(12, 26, 9)) &&
            c.IsMacdOscBullish(12, 26, 9)) || c.IsBreakingLowestClose(24));
        public override Predicate<IIndexedOhlcv> SellRule =>
            Rule.Create(c => c.Index > 10).
            And(c => c.Get<RateOfChange>(24)[c.Index].Tick > MinRateOfChange).
            And(c => c.IsEmaBullish(12) && c.IsEmaBullish(48) && c.IsEmaBullish(96)).
            //And(c => c.Get<MovingAverageConvergenceDivergence>(12, 26, 9)[c.Index].Tick.MacdHistogram.IsPositive()).
            And(c => ((c.Prev.IsMacdOscBullish(12, 26, 9) || 
                     c.Prev.Prev.IsMacdOscBullish(12, 26, 9) ||
                     c.Prev.Prev.Prev.IsMacdOscBullish(12, 26, 9)) &&
                     c.IsMacdOscBearish(12, 26, 9)) || c.IsBreakingHighestClose(24));

        public MacdBot(TradingBotManager botManager) : base(botManager, TradePeriod.Long)
        {
            this.botManager = botManager;
        }

        public override bool IsSell(TradeItem lastTrade) => lastTrade?.Type == TradeType.Sell;
    }
}
