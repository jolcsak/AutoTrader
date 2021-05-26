using System;
using Trady.Analysis;
using Trady.Analysis.Extension;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class AoBot : TradingBotBase, ITradingBot
    {
        public const int EMA_PERIOD = 48;

        private const int COOLDOWN_IN_MINUTES = 60;
        private const int PRICE_PERCENTAGE_CHANGE = 5;

        public override string Name => nameof(AoBot);
        public override Predicate<IIndexedOhlcv> BuyRule =>
            Rule.Create(c => c.IsFullStoBullishCross(14, 3, 3))
                .And(c => c.IsMacdOscBullish(12, 26, 9))
                .And(c => c.IsSmaOscBullish(10, 30))
                .And(c => c.IsAccumDistBullish())
                .And(c => c.IsEmaBullish(EMA_PERIOD));
        public override Predicate<IIndexedOhlcv> SellRule =>
            Rule.Create(c => c.IsFullStoBearishCross(14, 3, 3))
                .Or(c => c.IsMacdBearishCross(12, 24, 9))
                .Or(c => c.IsSmaBearishCross(10, 30));

        public AoBot(TradingBotManager botManager) : base(botManager, TradePeriod.Short, COOLDOWN_IN_MINUTES)
        {
            this.botManager = botManager;
        }
    }
}
