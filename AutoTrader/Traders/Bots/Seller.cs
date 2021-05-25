using AutoTrader.Db.Entities;

namespace AutoTrader.Traders.Bots
{
    public class Seller : ISeller
    {
        private static ISeller defaultSeller = new TradingBotBase(null, TradePeriod.Short);
        private static ISeller aoSeller = new AoBot(null);
        private static ISeller macdSeller = new MacdBot(null);
        private static ISeller rsiSeller = new RsiBot(null);
        private static ISeller spikeSeller = new SpikeBot(null);

        public SellType ShouldSell(ActualPrice actualPrice, TradeOrder tradeOrder, TradeItem lastTrade)
        {
            switch (tradeOrder.BotName)
            {
                case nameof(AoBot):
                    return aoSeller.ShouldSell(actualPrice, tradeOrder, lastTrade);
                case nameof(MacdBot):
                    return macdSeller.ShouldSell(actualPrice, tradeOrder, lastTrade);
                case nameof(RsiBot):
                    return rsiSeller.ShouldSell(actualPrice, tradeOrder, lastTrade);
                case nameof(SpikeBot):
                    return spikeSeller.ShouldSell(actualPrice, tradeOrder, lastTrade);
                default:
                    return defaultSeller.ShouldSell(actualPrice, tradeOrder, lastTrade);
            }
        }
    }
}
