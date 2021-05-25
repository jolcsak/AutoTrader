using AutoTrader.Db.Entities;

namespace AutoTrader.Traders.Bots
{
    public enum SellType
    {
        None,
        Profit,
        Loss
    }

    public interface ISeller
    {
        public SellType ShouldSell(ActualPrice actualPrice, TradeOrder tradeOrder, TradeItem lastTrade);
    }
}
