using AutoTrader.Db.Entities;
using System;
using Trady.Analysis;
using Trady.Analysis.Extension;
using Trady.Core.Infrastructure;

namespace AutoTrader.Traders.Bots
{
    public class SpikeBot : TradingBotBase, ITradingBot
    {
        public const int EMA_PERIOD = 48;

        private const int COOLDOWN_IN_MINUTES = 60;
        private const int PRICE_PERCENTAGE_CHANGE = 5;

        private const int STOP_PLOSS_PERCENTAGE = 4;
        private const int MAX_AGE_IN_HOURS = 6;

        public override string Name => nameof(SpikeBot);
        public override Predicate<IIndexedOhlcv> BuyRule => Rule.Create(c => c.Prev != null && c.ClosePricePercentageChange() + c.Prev.ClosePricePercentageChange() < -PRICE_PERCENTAGE_CHANGE).And(c => c.IsEmaBullish(EMA_PERIOD));
        public override Predicate<IIndexedOhlcv> SellRule => Rule.Create(c => c.Prev != null && c.ClosePricePercentageChange() + c.Prev.ClosePricePercentageChange() > PRICE_PERCENTAGE_CHANGE);

        public SpikeBot(TradingBotManager botManager) : base(botManager, TradePeriod.Short, COOLDOWN_IN_MINUTES)
        {
            this.botManager = botManager;
        }

        public override SellType ShouldSell(ActualPrice actualPrice, TradeOrder tradeOrder, TradeItem lastTrade)
        {
            if (actualPrice.BuyPrice >= tradeOrder.Price * TradeSettings.MinSellYield)
            {
                return SellType.Profit;
            }
            else
            {
                bool shouldShell = tradeOrder.ActualYield < STOP_PLOSS_PERCENTAGE || tradeOrder.BuyDate.AddHours(MAX_AGE_IN_HOURS) < DateTime.Now;
                if (shouldShell)
                {
                    return SellType.Loss;
                }
            }

            return SellType.None;
        }
    }
}
