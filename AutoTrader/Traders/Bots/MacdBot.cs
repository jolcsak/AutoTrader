using System;
using Trady.Analysis;
using Trady.Analysis.Extension;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class MacdBot : TradingBotBase, ITradingBot
    {
        public override string Name => nameof(MacdBot);
        public override Predicate<IIndexedOhlcv> BuyRule => Rule.Create(c => c.Prev != null && c.IsMacdBearishCross()).And(c => c.IsEmaBullish(TradingBotManager.EMA_PERIOD));
        public override Predicate<IIndexedOhlcv> SellRule => Rule.Create(c => c.Prev != null && c.IsMacdBullishCross());

        public MacdBot(TradingBotManager botManager) : base(botManager, TradePeriod.Long)
        {
            this.botManager = botManager;
        }
    }
}
