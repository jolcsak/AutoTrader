using System;
using Trady.Analysis;
using Trady.Analysis.Extension;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class SpikeBot : TradingBotBase, ITradingBot
    {
        private const int COOLDOWN_IN_MINUTES = 60;
        private const int PRICE_PERCENTAGE_CHANGE = 5;

        public override string Name => nameof(SpikeBot);
        public override Predicate<IIndexedOhlcv> BuyRule => Rule.Create(c => c.Prev != null && c.ClosePricePercentageChange() + c.Prev.ClosePricePercentageChange() < -PRICE_PERCENTAGE_CHANGE).And(c => c.IsEmaBullish(TradingBotManager.EMA_PERIOD));
        public override Predicate<IIndexedOhlcv> SellRule => Rule.Create(c => c.Prev != null && c.ClosePricePercentageChange() + c.Prev.ClosePricePercentageChange() > PRICE_PERCENTAGE_CHANGE);

        public SpikeBot(TradingBotManager botManager) : base(botManager, TradePeriod.Short, COOLDOWN_IN_MINUTES)
        {
            this.botManager = botManager;
        }
    }
}
